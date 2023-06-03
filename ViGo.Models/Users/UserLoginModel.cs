using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.Users
{
    public class WebUserLoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class MobileUserLoginModel
    {
        public string Phone { get; set; }
        public string FirebaseToken { get; set; }
    }
}
