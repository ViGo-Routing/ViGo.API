using FirebaseAdmin.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.Users;
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

        public async Task<User?> LoginAsync(WebUserLoginModel loginModel)
        {
            User? user = await work.Users.GetAsync(u =>
                !string.IsNullOrEmpty(u.Email) &&
                u.Email.ToLower().Trim()
                .Equals(loginModel.Email.ToLower().Trim())
                && (u.Role == UserRole.ADMIN || u.Role == UserRole.STAFF));

            if (user == null)
            {
                return null;
            }
            
            if (user.IsLockedOut &&
                DateTimeUtilities.GetDateTimeVnNow() <= user.LockedOutEnd)
            {
                throw new ApplicationException("Tài khoản của bạn đã bị khóa đăng nhập trong vòng 30 phút!\n" +
                    "Vui lòng thử đăng nhập lại sau " + (user.LockedOutEnd - DateTimeUtilities.GetDateTimeVnNow()).Value.Minutes
                    + " phút nữa");
            } else if (DateTimeUtilities.GetDateTimeVnNow() > user.LockedOutEnd)
            {
                user.IsLockedOut = false;
            }

            bool checkPassword = user.Password.Equals(loginModel.Password.Encrypt());
            if (!checkPassword)
            {
                // Wrong password
                if (user.FailedLoginCount == 4)
                {
                    // This time will be the fifth time
                    user.FailedLoginCount = 0;
                    user.IsLockedOut = true;
                    user.LockedOutStart = DateTimeUtilities.GetDateTimeVnNow();
                    user.LockedOutEnd = DateTimeUtilities.GetDateTimeVnNow().AddMinutes(30);

                    await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                    await work.SaveChangesAsync();

                    throw new ApplicationException("Tài khoản của bạn đã bị khóa đăng nhập trong vòng 30 phút!");
                } else
                {
                    user.FailedLoginCount++;
                    await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                    await work.SaveChangesAsync();
                }

                return null;
            } else
            {
                // Correct password
                if (user.FailedLoginCount > 0)
                {
                    user.FailedLoginCount = 0;
                    user.IsLockedOut = false;
                    user.LockedOutStart = null;
                    user.LockedOutEnd = null;

                    await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                    await work.SaveChangesAsync();
                }

                return user;
            }

        }

        public async Task<User> LoginAsync(MobileUserLoginModel loginModel)
        {
            try
            {
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(loginModel.FirebaseToken, checkRevoked: true);

                string uid = decodedToken.Uid;

                User? user = await work.Users.GetAsync(
                    u => !string.IsNullOrEmpty(u.FirebaseUid) &&
                        u.FirebaseUid.Equals(uid)
                    // Only Customer and Driver can login with Firebase
                    && (u.Role == UserRole.CUSTOMER || u.Role == UserRole.DRIVER));

                return user;

            } catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode == AuthErrorCode.RevokedIdToken)
                {
                    throw new ApplicationException("Token đã hết hiệu lực! Vui lòng đăng nhập lại");
                } else
                {
                    throw ex;
                }
            }
            
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
            //dto.Password.StringValidate(
            //    allowEmpty: false,
            //    emptyErrorMessage: "Mật khẩu không được bỏ trống!",
            //    minLength: 5,
            //    minLengthErrorMessage: "Mật khẩu phải có ít nhất 5 kí tự!",
            //    maxLength: 20,
            //    maxLengthErrorMessage: "Mật khẩu không được vượt quá 20 kí tự!");
            if (!Enum.IsDefined<UserRole>(dto.Role)
                || dto.Role != UserRole.CUSTOMER ||
                dto.Role != UserRole.DRIVER) {
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
                u => !string.IsNullOrEmpty(u.Phone) &&
                    u.Phone.Equals(dto.Phone));

            if (checkUser != null)
            {
                throw new ApplicationException("Số điện thoại đã được sử dụng!");
            }

            User checkFirebase = await work.Users.GetAsync(
                u => !string.IsNullOrEmpty(u.FirebaseUid)
                && u.FirebaseUid.Equals(dto.FirebaseUid));

            if (checkFirebase != null)
            {
                throw new ApplicationException("Thông tin Firebase UId không hợp lệ!");
            }

            User newUser = new User
            {
                Name = dto.Name,
                Phone = dto.Phone,
                //Password = dto.Password.Encrypt(),
                Role = dto.Role,
                FirebaseUid = dto.FirebaseUid,
                Status = dto.Role == UserRole.DRIVER 
                    ? UserStatus.PENDING 
                    : UserStatus.ACTIVE
            };

            await work.Users.InsertAsync(newUser, isSelfCreatedEntity: true);

            Wallet wallet = new Wallet
            {
                UserId = newUser.Id,
                Balance = 0,
                Type = WalletType.PERSONAL,
                Status = WalletStatus.ACTIVE,
                CreatedBy = newUser.Id,
                UpdatedBy = newUser.Id,
            };

            await work.Wallets.InsertAsync(wallet, isManuallyAssignTracking: true);

            await work.SaveChangesAsync();

            newUser.Password = "";

            return newUser;
        }
        public async Task<User> GetUserByIdAsync(Guid id)
        {
            User user = await work.Users.GetAsync(id);
            return user;
        }

        public async Task<User> UpdateUserAsync(Guid id, UserUpdateModel userUpdate)
        {
            User currentUser = await GetUserByIdAsync(id);

            if (currentUser != null)
            {
                if (userUpdate.Email != null)
                {
                    userUpdate.Email.IsEmail("Email không hợp lệ!");
                    currentUser.Email = userUpdate.Email;
                }
                if (userUpdate.Password != null)
                {
                    currentUser.Password.StringValidate(
                        allowEmpty: false,
                        emptyErrorMessage: "Mật khẩu không được bỏ trống!",
                        minLength: 5,
                        minLengthErrorMessage: "Mật khẩu phải có ít nhất 5 kí tự!",
                        maxLength: 20,
                        maxLengthErrorMessage: "Mật khẩu không được vượt quá 20 kí tự!");
                    currentUser.Password = userUpdate.Password.Encrypt();
                }
                if (userUpdate.Name != null)
                {
                    currentUser.Name.StringValidate(
                        allowEmpty: false,
                        emptyErrorMessage: "Họ tên không được bỏ trống!",
                        minLength: 5,
                        minLengthErrorMessage: "Họ tên phải có ít nhất 5 kí tự!",
                        maxLength: 50,
                        maxLengthErrorMessage: "Họ tên không được vượt quá 50 kí tự!");
                    currentUser.Name = userUpdate.Name;
                }
                if (userUpdate.Gender != null)
                {
                    currentUser.Gender = userUpdate.Gender;
                }
                if (userUpdate.DateOfBirth != null)
                {
                    currentUser.DateOfBirth.Value.DateTimeValidate(maximum: DateTimeUtilities.GetDateTimeVnNow(), maxErrorMessage: "Ngày sinh không hợp lệ!");
                    currentUser.DateOfBirth = userUpdate.DateOfBirth;
                }
                if (userUpdate.AvatarUrl != null)
                {
                    currentUser.AvatarUrl = userUpdate.AvatarUrl;
                }
            }

            await work.Users.UpdateAsync(currentUser!);
            await work.SaveChangesAsync();
            return currentUser!;
        }
    }
}
