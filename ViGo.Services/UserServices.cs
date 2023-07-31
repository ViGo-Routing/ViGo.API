using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.Users;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Models.QueryString.Pagination;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Validator;
using ViGo.Models.QueryString;
using ViGo.Models.Notifications;

namespace ViGo.Services
{
    public class UserServices : UseNotificationServices
    {
        public UserServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<User?> LoginAsync(WebUserLoginModel loginModel,
            CancellationToken cancellationToken)
        {
            User? user = await work.Users.GetAsync(u =>
                !string.IsNullOrEmpty(u.Email) &&
                u.Email.ToLower().Trim()
                .Equals(loginModel.Email.ToLower().Trim())
                && (u.Role == UserRole.ADMIN || u.Role == UserRole.STAFF), 
                cancellationToken: cancellationToken);

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
                    await work.SaveChangesAsync(cancellationToken);

                    throw new ApplicationException("Tài khoản của bạn đã bị khóa đăng nhập trong vòng 30 phút!");
                } else
                {
                    user.FailedLoginCount++;
                    await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                    await work.SaveChangesAsync(cancellationToken);
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
                    await work.SaveChangesAsync(cancellationToken);
                }

                return user;
            }

        }

        public async Task<User?> LoginAsync(MobileUserLoginModel loginModel,
            CancellationToken cancellationToken)
        {
            try
            {
                if (loginModel.Role == UserRole.ADMIN)
                {
                    throw new ApplicationException("Vai trò đăng nhập không hợp lệ!!");
                }

                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(loginModel.FirebaseToken, checkRevoked: true, 
                    cancellationToken: cancellationToken);

                string uid = decodedToken.Uid;

                User? user = await work.Users.GetAsync(
                    u => !string.IsNullOrEmpty(u.FirebaseUid) &&
                        u.FirebaseUid.Equals(uid)
                    // Only Customer and Driver can login with Firebase
                    && (u.Role == loginModel.Role), 
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    return user;
                }
                if (!string.IsNullOrEmpty(user.Phone) 
                    && !user.Phone.Equals(loginModel.Phone))
                {
                    throw new ApplicationException("Thông tin số điện thoại không trùng khớp!!");
                }

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

        public async Task<IPagedEnumerable<User>> GetUsersAsync(PaginationParameter pagination,
            UserSortingParameters sorting, UserFilterParameters filters,
            HttpContext context, CancellationToken cancellationToken)
        {
            IEnumerable<User> users
                = await work.Users.GetAllAsync(cancellationToken: cancellationToken);

            users = FilterUsers(users, filters);

            users = users.Sort(sorting.OrderBy);
            int totalRecords = users.Count();

            return users.ToPagedEnumerable(pagination.PageNumber, 
                pagination.PageSize, totalRecords, context, isOriginalSource: true);
        }

        public async Task<User> RegisterAsync(UserRegisterModel dto,
            CancellationToken cancellationToken)
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
                || (dto.Role != UserRole.CUSTOMER &&
                dto.Role != UserRole.DRIVER)) {
                throw new ApplicationException("Vai trò người dùng không hợp lệ!");
            }

            if (dto.Role == UserRole.CUSTOMER)
            {
                dto.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Họ tên không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Họ tên phải có ít nhất 5 kí tự!",
                maxLength: 50,
                maxLengthErrorMessage: "Họ tên không được vượt quá 50 kí tự!");
            }
            
            User checkUser = await work.Users.GetAsync(
                u => !string.IsNullOrEmpty(u.Phone) &&
                    u.Phone.Equals(dto.Phone), 
                cancellationToken: cancellationToken);

            if (checkUser != null)
            {
                throw new ApplicationException("Số điện thoại đã được sử dụng!");
            }

            User checkFirebase = await work.Users.GetAsync(
                u => !string.IsNullOrEmpty(u.FirebaseUid)
                && u.FirebaseUid.Equals(dto.FirebaseUid), 
                cancellationToken: cancellationToken);

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

            await work.Users.InsertAsync(newUser, isSelfCreatedEntity: true, 
                cancellationToken: cancellationToken);

            Wallet wallet = new Wallet
            {
                UserId = newUser.Id,
                Balance = 0,
                Type = WalletType.PERSONAL,
                Status = WalletStatus.ACTIVE,
                CreatedBy = newUser.Id,
                UpdatedBy = newUser.Id,
            };

            await work.Wallets.InsertAsync(wallet, isManuallyAssignTracking: true, 
                cancellationToken: cancellationToken);

            await work.SaveChangesAsync(cancellationToken);

            newUser.Password = "";

            return newUser;
        }

        public async Task<UserViewModel> GetUserByIdAsync(Guid id)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                id = IdentityUtilities.GetCurrentUserId();
            }

            User user = await work.Users.GetAsync(id);
            return new UserViewModel(user);
        }

        public async Task<UserViewModel?> GetUserByPhoneNumberAsync(
            string phoneNumber, CancellationToken cancellationToken)
        {
            User user = await work.Users.GetAsync(
                u => u.Phone != null && u.Phone.Equals(phoneNumber.Trim()),
                cancellationToken: cancellationToken);

            if (user is null)
            {
                return null;
            }

            return new UserViewModel(user);

        }

        public async Task<User> UpdateUserAsync(Guid id, 
            UserUpdateModel userUpdate)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                if (!id.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!");
                }
            }

            User currentUser = await work.Users.GetAsync(id);

            if (currentUser != null)
            {
                if (userUpdate.Email != null)
                {
                    userUpdate.Email.IsEmail("Email không hợp lệ!");
                    currentUser.Email = userUpdate.Email;
                }
                if (userUpdate.Password != null
                    && !string.IsNullOrEmpty(userUpdate.Password)
                    && !currentUser.Password!.Decrypt().Equals(userUpdate.Password))
                {
                    userUpdate.Password.StringValidate(
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
                    userUpdate.Name.StringValidate(
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
                    userUpdate.DateOfBirth.Value.DateTimeValidate(maximum: DateTimeUtilities.GetDateTimeVnNow(), maxErrorMessage: "Ngày sinh không hợp lệ!");
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

        public async Task<User> UpdateUserFcmToken(UserUpdateFcmTokenModel model,
            CancellationToken cancellationToken)
        {
            User? user = await work.Users.GetAsync(model.Id, cancellationToken: cancellationToken);
            if (user is null)
            {
                throw new ApplicationException("Thông tin người dùng không hợp lệ! Vui lòng kiểm tra lại!!");
            }

            if (string.IsNullOrEmpty(model.FcmToken))
            {
                throw new ApplicationException("FCM Token không được bỏ trống!!");
            }

            if (!IdentityUtilities.IsAdmin() && !IdentityUtilities.IsStaff()
                && !IdentityUtilities.GetCurrentUserId().Equals(model.Id))
            {
                // Only Admin, Staff or current logged in user can update user's fcm token
                throw new ApplicationException("Bạn không được phép thực hiện hành động này!!");
            }

            user.FcmToken = model.FcmToken;

            await work.Users.UpdateAsync(user);
            await work.SaveChangesAsync(cancellationToken);

            return user;
        }

        public async Task<string?> GetUserFcmToken(Guid userId, CancellationToken cancellationToken)
        {
            User? user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            if (user is null)
            {
                throw new ApplicationException("Người dùng không tồn tại!!");
            }
            return user.FcmToken;
        }

        public async Task<UserViewModel> ChangeUserStatus(Guid id, 
            UserChangeStatusModel statusChange, CancellationToken cancellationToken)
        {
            var currentUser = await work.Users.GetAsync(id, 
                cancellationToken: cancellationToken);
            if (currentUser is null)
            {
                throw new ApplicationException("User không tồn tại!");
            }
            else
            {
                currentUser.Status = statusChange.Status;
            }

            await work.Users.UpdateAsync(currentUser); 
            await work.SaveChangesAsync(cancellationToken);

            if (currentUser.Status == UserStatus.ACTIVE ||
                currentUser.Status == UserStatus.REJECTED)
            {
                string? fcmToken = currentUser.FcmToken;
                if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                {
                    NotificationCreateModel notification = new NotificationCreateModel()
                    {
                        UserId = currentUser.Id,
                        Type = NotificationType.SPECIFIC_USER,
                    };

                    switch(currentUser.Status)
                    {
                        case UserStatus.ACTIVE:
                            notification.Title = "Tài khoản của bạn đã được kích hoạt!";
                            notification.Description = currentUser.Role == UserRole.DRIVER ?
                                "Các thông tin của bạn đã được duyệt, hãy vào ViGo để chọn chuyến đi đầu tiên nào!!"
                                : currentUser.Role == UserRole.CUSTOMER ?
                                "Hãy vào ViGo để đặt lịch hành trình đầu tiên của bạn nhé!"
                                : "Các thông tin của bạn đã được duyệt!";
                            break;
                        case UserStatus.REJECTED:
                            notification.Title = "Tài khoản của bạn đã bị từ chối!";
                            notification.Description = currentUser.Role == UserRole.DRIVER ?
                                "Các thông tin của bạn đã không được duyệt, hãy vào ViGo để cập nhật lại thông tin của bạn nhé!!"
                                : "Các thông tin của bạn đã không được duyệt!";
                            break;
                    }

                    Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                        {
                            { "action", NotificationAction.Login },
                        };

                    await notificationServices.CreateFirebaseNotificationAsync(
                        notification, fcmToken, dataToSend, cancellationToken);
                }
                
            }
            UserViewModel userView = new UserViewModel(currentUser);
            return userView;
            
        }

        private IEnumerable<User> FilterUsers(IEnumerable<User> users,
            UserFilterParameters filters)
        {
            if (filters.Role != null && !string.IsNullOrWhiteSpace(filters.Role))
            {
                IEnumerable<UserRole> userRoles = new List<UserRole>();

                var roles = filters.Role.Split(",");
                foreach (string role in roles)
                {
                    if (Enum.TryParse(typeof(UserRole), role.Trim(),
                        true, out object? result))
                    {
                        if (result != null)
                        {
                            userRoles = userRoles.Append((UserRole)result);
                        }
                    }
                }

                if (userRoles.Any())
                {
                    users = users.Where(b => userRoles.Contains(b.Role));
                }
            }

            return users;
        }

        public async Task<UserAnalysisModel> GetUserAnalysisAsync(CancellationToken cancellationToken)
        {
            IEnumerable<User> users = await work.Users.GetAllAsync(
                cancellationToken: cancellationToken);

            UserAnalysisModel analysisModel = new UserAnalysisModel()
            {
                TotalActiveUsers = users.Count(u => u.Status == UserStatus.ACTIVE),
                TotalInactiveUsers = users.Count(u => u.Status == UserStatus.INACTIVE),
                TotalBannedUsers = users.Count(u => u.Status == UserStatus.BANNED),
                TotalPendingDrivers = users.Count(u => u.Status == UserStatus.PENDING),
                TotalRejectedDrivers = users.Count(u => u.Status == UserStatus.REJECTED),
                TotalCustomers = users.Count(u => u.Role == UserRole.CUSTOMER),
                TotalDrivers = users.Count(u => u.Role == UserRole.DRIVER),
                TotalNewDriversInCurrentMonth = users.Count(
                    u => u.Role == UserRole.DRIVER
                    && u.CreatedTime.IsInCurrentMonth()),
                TotalNewCustomersInCurrentMonth = users.Count(
                    u => u.Role == UserRole.CUSTOMER
                    && u.CreatedTime.IsInCurrentMonth())
            };

            return analysisModel;
        }
    }
}
