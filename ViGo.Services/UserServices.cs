using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class UserServices : BaseServices<User>
    {
        public UserServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<User> LoginAsync(string phone, string password)
        {
            User? user = null;

            //try
            //{
                user = await work.Users.GetAsync(
                    u => u.Phone.ToLower().Trim()
                    .Equals(phone.ToLower().Trim())
                    && u.Password.Equals(password.Encrypt()));

                //return user;
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception(ex.Message, ex.InnerException);
            //}
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

        public async Task<User> RegisterAsync(UserRegisterModel dto)
        {
            dto.Phone.IsPhoneNumber("Số điện thoại không hợp lệ!");
            dto.Password.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Mật khẩu không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Mật khẩu phải có ít nhất 5 kí tự!",
                maxLength: 20,
                maxLengthErrorMessage: "Mật khẩu không được vượt quá 20 kí tự!");
            if (!Enum.IsDefined<UserRole>(dto.Role)) {
                throw new ApplicationException("Vai trò người dùng không hợp lệ!");
            }

            dto.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Họ tên không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Họ tên phải có ít nhất 5 kí tự!",
                maxLength: 50,
                maxLengthErrorMessage: "Họ tên không được vượt quá 50 kí tự!");


            User checkUser = await work.Users.GetAsync(
                u => u.Phone.Equals(dto.Phone));
            if (checkUser != null)
            {
                throw new ApplicationException("Số điện thoại đã được sử dụng!");
            }

            User newUser = new User
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Password = dto.Password.Encrypt(),
                Role = dto.Role,
                Status = UserStatus.UNVERIFIED
            };

            await work.Users.InsertAsync(newUser, isSelfCreatedEntity: true);
            await work.SaveChangesAsync();

            newUser.Password = "";

            return newUser;
        }
    }
}
