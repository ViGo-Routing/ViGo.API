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
            User user = null;

            try
            {
                user = await work.Users.GetAsync(
                    u => u.Email.ToLower().Trim()
                    .Equals(email.ToLower().Trim())
                    && u.Password.Equals(password.Encrypt()));

                return user;
            } catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
            return user;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            //User user = new User
            //{
            //    //Id = Guid.NewGuid(),
            //    Name = "Admin Trần Phong",
            //    Email = "phongntse150974@fpt.edu.vn",
            //    Password = SecurityUtilities.Encrypt("123456789"),
            //    Role = Domain.Enumerations.UserRole.ADMIN,
            //    Status = Domain.Enumerations.UserStatus.ACTIVE,
            //    IsLockedOut = false
            //};

            ////user.CreatedBy = user.Id;
            ////user.UpdatedBy = user.Id;

            //await work.Users.InsertAsync(user, true);
            //await work.SaveChangesAsync();

            IEnumerable<User> users
                = await work.Users.GetAllAsync();

            return users;
        }
    }
}
