using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;

namespace ViGo.Services
{
    public class UserServices : BaseServices<User>
    {
        public UserServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<User> Login(string email, string password)
        {
            User? user = null;

            try
            {
                user = await work.Users.GetAsync(
                    u => u.Phone.ToLower().Trim()
                    .Equals(email.ToLower().Trim())
                    && u.Password.Equals(password.Encrypt()));

                //return user;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
            return user;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            //List<User> _users = new List<User>
            //{
            //    new User
            //    {
            //        //Id = Guid.NewGuid(),
            //        Name = "Customer Trần Phong",
            //        Email = "phong_customer@gmail.com",
            //        Password = SecurityUtilities.Encrypt("123456789"),
            //        Role = Domain.Enumerations.UserRole.CUSTOMER,
            //        Status = Domain.Enumerations.UserStatus.ACTIVE,
            //        IsLockedOut = false
            //    },

            //    new User
            //    {
            //        //Id = Guid.NewGuid(),
            //        Name = "Driver Trần Phong",
            //        Email = "phong_driver@gmail.com",
            //        Password = SecurityUtilities.Encrypt("123456789"),
            //        Role = Domain.Enumerations.UserRole.DRIVER,
            //        Status = Domain.Enumerations.UserStatus.ACTIVE,
            //        IsLockedOut = false
            //    },
            //};

            //////user.CreatedBy = user.Id;
            //////user.UpdatedBy = user.Id;

            //await work.Users.InsertAsync(_users, true);
            //await work.SaveChangesAsync();

            IEnumerable<User> users
                = await work.Users.GetAllAsync();

            return users;
        }
    }
}
