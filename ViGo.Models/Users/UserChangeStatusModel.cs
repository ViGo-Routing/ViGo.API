using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Users
{
    public class UserChangeStatusModel
    {
        public UserStatus Status { get; set; }
    }
}
