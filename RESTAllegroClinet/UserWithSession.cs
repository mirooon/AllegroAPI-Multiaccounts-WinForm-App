using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTAllegroClinet
{
    public class UserWithSession : User
    {
        public UserWithSession(User user)
        {
            this.id = user.id;
            this.login = user.login;
            this.password = user.password;
        }
        public string sessionHandle { get; set; }
    }
}
