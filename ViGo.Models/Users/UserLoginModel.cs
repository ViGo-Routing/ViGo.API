using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

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
        public UserRole Role { get; set; } = UserRole.CUSTOMER;
        //public string FirebaseToken { get; set; }
        public string Password { get; set; }
    }
}
