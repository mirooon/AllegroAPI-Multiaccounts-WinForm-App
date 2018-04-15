using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RESTAllegroClinet
{
    public partial class Form1 : Form
    {
        const string webApiKey = "6cbb6c56";
        const int localVersion = 1523602068; // wersja API
        const string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog = RESTAllegroClient; Integrated Security = True; ";
        string sysStatus;

        AllegroWebApi.servicePortClient client = new AllegroWebApi.servicePortClient();

        List<UserWithSession> UsersWithSession;

        public Form1()
        {
            InitializeComponent();
          //  this.ClientSize = new System.Drawing.Size(700,400);
            client = new AllegroWebApi.servicePortClient();
            UsersWithSession = new List<UserWithSession>();
            
            //AllegroWebApi.SysStatusType[] response = client.doQueryAllSysStatus(1, webApiKey);
            //foreach (AllegroWebApi.SysStatusType sst in response)
            //{
            //    listBox1.Items.Add(
            //            "Country id=" + sst.countryId.ToString("000") +
            //            "program-version=" + sst.programVersion +
            //            " cats-version=" + sst.catsVersion + "" +
            //            " api-version=" + sst.apiVersion + "" +
            //            " attrib-version=" + sst.attribVersion + "" +
            //            " form-sell-version=" + sst.formSellVersion + "" +
            //            " site-version=" + sst.siteVersion + "" +
            //            " var-key=" + sst.verKey + ""
            //          );
            //}
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            List<User> Users;
            using (AllegroAPIClientAccountsEntities AllegroEntities = new AllegroAPIClientAccountsEntities())
            {
                Users = AllegroEntities.Users.ToList();
            }
            if (Users.Count() != 0)
            {
                foreach (var user in Users)
                {
                    int countryCode = 1;
                    long hashoffset = 0;
                    long serverTime = 0;    //deklaracje zmiennych zwracanych przez funkcję doLogin
                    string sessionHandle;
                    using (SHA256 sha256 = new SHA256Managed())
                    {
                        string pass = Cryptography.Decryptdata(user.password);
                        byte[] passwordHash = sha256.ComputeHash(Encoding.ASCII.GetBytes(pass));
                        string encodedPassword = Convert.ToBase64String(passwordHash);

                        try //logowanie może się przecież nie powieść - wtedy zostanie zwrócony wyjątek, który musimy złapać
                        {
                            sessionHandle =
                               client.doLoginEnc(
                               user.login,      //login został wpisany w polu tekstowym
                               encodedPassword,      //hasło
                               countryCode,                  //parametr "countrycode", lista wartości w dokumentacji
                               webApiKey,          //klucz webapi - zadeklarowany wcześniej jako stała
                               localVersion,         //klucz wersji - jw.
                               out hashoffset,     //  
                               out serverTime);    // dodatkowe wartości zwracane przez funkcję.
                        }
                        catch (Exception E)
                        {
                            listBox1.Items.Add("Logowanie " + user.login + " nie powiodło się");     //jeżeli logowanie nie powiodło się - wyświetlamy komunikat
                            listBox1.Items.Add(E.ToString());
                            return;
                        }
                    }
                    UserWithSession userWithSession = new UserWithSession(user);
                    userWithSession.sessionHandle = sessionHandle;
                    UsersWithSession.Add(userWithSession);
                    LoggedUsers.Items.Add(userWithSession.login);
                }
                    DisplaySellItems();
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if ((!string.IsNullOrWhiteSpace(textBoxLogin.Text) && !string.IsNullOrWhiteSpace(textBoxPassword.Text)) == true)
            {
                string login = textBoxLogin.Text;
                string password = textBoxPassword.Text;
                int countryCode = 1;
                long hashoffset = 0; long serverTime = 0;    //deklaracje zmiennych zwracanych przez funkcję doLogin
                using (SHA256 sha256 = new SHA256Managed())
                {
                    byte[] passwordHash = sha256.ComputeHash(Encoding.ASCII.GetBytes(password));
                    string encodedPassword = Convert.ToBase64String(passwordHash);
                    try //logowanie może się przecież nie powieść - wtedy zostanie zwrócony wyjątek, który musimy złapać
                    {
                        string sessionHandle =
                            client.doLoginEnc(

                                login,      //login został wpisany w polu tekstowym
                            encodedPassword,      //hasło
                            countryCode,                  //parametr "countrycode", lista wartości w dokumentacji
                            webApiKey,          //klucz webapi - zadeklarowany wcześniej jako stała
                            localVersion,         //klucz wersji - jw.
                            out hashoffset,     //  
                            out serverTime);    // dodatkowe wartości zwracane przez funkcję.

                        using (AllegroAPIClientAccountsEntities AllegroEntities = new AllegroAPIClientAccountsEntities())
                        {
                            User user = new User()
                            {
                                login = login,
                                password = Cryptography.Encryptdata(password),
                            };
                            if (AllegroEntities.Users.Any(a => a.login == user.login))
                            {
                                MessageBox.Show("Taki użytkownik już istnieje.");
                            }
                            else
                            {
                                AllegroEntities.Users.Add(user);

                                try
                                {
                                    AllegroEntities.SaveChanges();
                                    UserWithSession userWithSession = new UserWithSession(user);
                                    userWithSession.sessionHandle = sessionHandle;
                                    UsersWithSession.Add(userWithSession);
                                    MessageBox.Show("Dodano pomyślnie.");
                                    LoggedUsers.Items.Add(userWithSession.login);
                                }
                                catch (DbEntityValidationException ex)
                                {
                                    foreach (var entityValidationErrors in ex.EntityValidationErrors)
                                    {
                                        foreach (var validationError in entityValidationErrors.ValidationErrors)
                                        {
                                            Debug.Write("Property: " + validationError.PropertyName + " Error: " + validationError.ErrorMessage);
                                        }
                                    }
                                }

                            }

                        }


                    }
                    catch (Exception E)
                    {
                        listBox1.Items.Add("Logowanie nie powiodło się");     //jeżeli logowanie nie powiodło się - wyświetlamy komunikat
                        listBox1.Items.Add(E.ToString());
                        return;
                    }
                }
            }
            else
            {
                MessageBox.Show("Nie podano loginu lub hasła");
            }

        }
        public void DisplaySellItems()
        {

            AllegroWebApi.MyAccountStruct2[] accountStructTable;
            foreach (var user in UsersWithSession)
            {
                try
                {
                    accountStructTable = client.doMyAccount2(
                        user.sessionHandle,
                        "sell",
                        0,
                        new long[0],
                        100);
                }
                catch
                {
                    listBox1.Items.Add("Wystąpił błąd w funkcji doMyAccountStruct2");
                    return;
                }

                foreach (AllegroWebApi.MyAccountStruct2 accountStruct in accountStructTable)
                {
                    dataGridView1.Rows.Add(accountStruct.myAccountArray[24],
                        accountStruct.myAccountArray[9], accountStruct.myAccountArray[17], accountStruct.myAccountArray[33]);
                }
            }
        }

    }
}
