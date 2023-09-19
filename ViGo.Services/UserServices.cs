using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.Users;
using ViGo.Models.GoogleMaps;
using ViGo.Models.Notifications;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Validator;

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
            }
            else if (DateTimeUtilities.GetDateTimeVnNow() > user.LockedOutEnd)
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
                }
                else
                {
                    user.FailedLoginCount++;
                    await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                    await work.SaveChangesAsync(cancellationToken);
                }

                return null;
            }
            else
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

                if (string.IsNullOrEmpty(loginModel.Password) &&
                    string.IsNullOrEmpty(loginModel.FirebaseToken))
                {
                    throw new ApplicationException("Thiếu thông tin đăng nhập!!");
                }

                User? user = await work.Users.GetAsync(
                    u => u.Phone != null && !string.IsNullOrEmpty(u.Phone)
                        && u.Phone.Equals(loginModel.Phone)
                        && u.Role == loginModel.Role,
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    return user;
                }

                if (!string.IsNullOrEmpty(loginModel.Password))
                {
                    // login with password

                    //if (!string.IsNullOrEmpty(user.Phone) 
                    //    && !user.Phone.Equals(loginModel.Phone))
                    //{
                    //    throw new ApplicationException("Thông tin số điện thoại không trùng khớp!!");
                    //}
                    if (user.IsLockedOut &&
                    DateTimeUtilities.GetDateTimeVnNow() <= user.LockedOutEnd)
                    {
                        throw new ApplicationException("Tài khoản của bạn đã bị khóa đăng nhập trong vòng 30 phút!\n" +
                            "Vui lòng thử đăng nhập lại sau " + (user.LockedOutEnd - DateTimeUtilities.GetDateTimeVnNow()).Value.Minutes
                            + " phút nữa");
                    }
                    else if (DateTimeUtilities.GetDateTimeVnNow() > user.LockedOutEnd)
                    {
                        user.IsLockedOut = false;
                    }

                    if (string.IsNullOrEmpty(user.Password))
                    {
                        throw new ApplicationException("Tài khoản này không được phép đăng nhập bằng mật khẩu! Vui lòng sử dụng OTP!");
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
                        }
                        else
                        {
                            user.FailedLoginCount++;
                            await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                            await work.SaveChangesAsync(cancellationToken);
                        }

                        return null;
                    }
                    else
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
                else if (!string.IsNullOrEmpty(loginModel.FirebaseToken))
                {
                    FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance
                    .VerifyIdTokenAsync(loginModel.FirebaseToken, checkRevoked: true,
                        cancellationToken: cancellationToken);

                    string uid = decodedToken.Uid;

                    //user = await work.Users.GetAsync(
                    //    u => !string.IsNullOrEmpty(u.FirebaseUid) &&
                    //        u.FirebaseUid.Equals(uid)
                    //        && u.Phone.Equals(loginModel.Phone)
                    //    // Only Customer and Driver can login with Firebase
                    //    && (u.Role == loginModel.Role),
                    //    cancellationToken: cancellationToken);

                    if (string.IsNullOrEmpty(user.FirebaseUid))
                    {
                        //throw new ApplicationException("Tài khoản này không được phép đăng nhập bằng OTP! Vui lòng sử dụng mật khẩu!");
                        user.FirebaseUid = uid;
                        await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);

                        await work.SaveChangesAsync(cancellationToken);
                    }
                    return user;

                    //bool checkFirebaseUid = user.FirebaseUid.Equals(uid);

                    //if (checkFirebaseUid)
                    //{
                    //    return user;

                    //}
                    //return null;

                }

                return null;
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode == AuthErrorCode.RevokedIdToken)
                {
                    throw new ApplicationException("Token đã hết hiệu lực! Vui lòng đăng nhập lại");
                }
                else
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

            if (string.IsNullOrEmpty(dto.Password) && string.IsNullOrEmpty(dto.FirebaseUid))
            {
                throw new ApplicationException("Thông tin đăng ký không hợp lệ!!");
            }

            if (!string.IsNullOrEmpty(dto.Password))
            {
                dto.Password.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Mật khẩu không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Mật khẩu phải có ít nhất 5 kí tự!",
                maxLength: 20,
                maxLengthErrorMessage: "Mật khẩu không được vượt quá 20 kí tự!");

            }
            else if (!string.IsNullOrEmpty(dto.FirebaseUid))
            {
                //User checkFirebase = await work.Users.GetAsync(
                //    u => !string.IsNullOrEmpty(u.FirebaseUid)
                //    && u.FirebaseUid.Equals(dto.FirebaseUid)
                //    && u.Role == dto.Role,
                //    cancellationToken: cancellationToken);

                //if (checkFirebase != null)
                //{
                //    throw new ApplicationException("Thông tin Firebase UId không hợp lệ!");
                //}
            }

            if (!Enum.IsDefined(dto.Role)
                || (dto.Role != UserRole.CUSTOMER &&
                dto.Role != UserRole.DRIVER))
            {
                throw new ApplicationException("Vai trò người dùng không hợp lệ!");
            }

            if (dto.Role == UserRole.CUSTOMER)
            {
                if (dto.Name is null)
                {
                    throw new ApplicationException("Họ tên không được bỏ trống!");
                }

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
                    u.Phone.Equals(dto.Phone)
                    && u.Role == dto.Role,
                cancellationToken: cancellationToken);

            if (checkUser != null)
            {
                throw new ApplicationException("Số điện thoại đã được sử dụng!");

            }

            User newUser = new User
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Password = dto.Password?.Encrypt(),
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

        public async Task<UserViewModel?> GetUserByIdAsync(Guid id)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                id = IdentityUtilities.GetCurrentUserId();
            }

            User? user = await work.Users.GetAsync(id);

            if (user is null)
            {
                return null;
            }

            return new UserViewModel(user);
        }

        public async Task<UserViewModel?> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            Guid currentUserId = IdentityUtilities.GetCurrentUserId();
            User? currentUser = await work.Users.GetAsync(currentUserId, cancellationToken: cancellationToken);

            if (currentUser is null)
            {
                return null;
            }

            return new UserViewModel(currentUser);
        }

        public async Task<UserViewModel> GetBookingDetailCustomerAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(
                bookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!");
            }
            Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId, cancellationToken: cancellationToken);
            User user = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);
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

                    if (!userUpdate.Email.Equals(currentUser.Email))
                    {
                        User? checkEmail = (await work.Users
                        .GetAllAsync(query => query.Where(
                            u => !string.IsNullOrEmpty(u.Email)
                             && u.Email.Equals(userUpdate.Email)))).FirstOrDefault();

                        if (checkEmail != null)
                        {
                            throw new ApplicationException("Email đã được sử dụng!");
                        }

                        currentUser.Email = userUpdate.Email;
                    }
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
                    userUpdate.DateOfBirth.Value.DateTimeValidate(maximum: 
                        DateTimeUtilities.GetDateTimeVnNow().AddYears(-12), maxErrorMessage: "Ngày sinh không hợp lệ! Người dùng phải ít nhất 12 tuổi!");
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
                throw new ApplicationException("Người dùng không tồn tại!");
            }
            else
            {
                if (statusChange.Status == UserStatus.ACTIVE ||
                    statusChange.Status == UserStatus.REJECTED)
                {
                    IEnumerable<UserLicense> userLicenses = await work.UserLicenses
                        .GetAllAsync(query => query.Where(
                            ul => ul.UserId.Equals(id)
                            && ul.Status == UserLicenseStatus.PENDING), cancellationToken: cancellationToken);
                    if (userLicenses.Any())
                    {
                        throw new ApplicationException("Vui lòng xem xét duyệt / từ chối các " +
                            "giấy tờ của người dùng trước khi thay đổi trạng thái!");
                    }
                }
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

                    switch (currentUser.Status)
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

        public async Task<SingleUserAnalysisModel?> GetSingleUserAnalysisAsync(Guid userId,
            CancellationToken cancellationToken)
        {
            User? user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            if (user is null)
            {
                throw new ApplicationException("Người dùng không tồn tại!!");
            }

            if (user.Role == UserRole.DRIVER)
            {
                IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                    .GetAllAsync(query => query.Where(
                        d => d.DriverId.HasValue && d.DriverId.Equals(userId)
                        && (d.Status == BookingDetailStatus.COMPLETED ||
                        d.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF ||
                        d.Status == BookingDetailStatus.PENDING_PAID)),
                        cancellationToken: cancellationToken);

                return new SingleUserAnalysisModel
                {
                    TotalCompletedTrips = bookingDetails.Count()
                };
            }
            return null;
        }

        public async Task<IEnumerable<UserViewModel>> GetAvailableDriversForTrip(
            Guid bookingDetailId, CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(bookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!");
            }

            IEnumerable<User> drivers = await work.Users.GetAllAsync(query => query.Where(
                u => u.Role == UserRole.DRIVER && u.Status == UserStatus.ACTIVE),
                cancellationToken: cancellationToken);

            IEnumerable<UserViewModel> availableDrivers = new List<UserViewModel>();
            if (!drivers.Any())
            {
                return availableDrivers;
            }

                    // Has trips in day
            Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);

            TimeSpan bookingDetailEndTime = DateTimeUtilities.CalculateTripEndTime(
                        bookingDetail.CustomerDesiredPickupTime, booking.Duration);

            Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId,
                cancellationToken: cancellationToken);

            DriverTrip addedTrip = new DriverTrip
            {
                Id = bookingDetail.Id,
                BeginTime = bookingDetail.CustomerDesiredPickupTime,
                EndTime = bookingDetailEndTime,
                StartLocation = new GoogleMapPoint
                {
                    Latitude = startStation.Latitude,
                    Longitude = startStation.Longitude
                },
                EndLocation = new GoogleMapPoint
                {
                    Latitude = endStation.Latitude,
                    Longitude = endStation.Longitude
                }
            };

            foreach (User driver in drivers)
            {
                DriverTripsOfDate driverTripsOfDate = await GetDriverSchedulesInDateAsync(
                    driver.Id, bookingDetail.Date, cancellationToken);
                if (driverTripsOfDate.Trips.Count == 0)
                {
                    // No trips
                    // Driver is free to be assigned
                    availableDrivers = availableDrivers.Append(new UserViewModel(driver));
                } else
                {
                   
                    IEnumerable<DriverTrip> addedTrips = driverTripsOfDate.Trips.Append(addedTrip)
                        .OrderBy(t => t.BeginTime);
                    LinkedList<DriverTrip> addedTripsAsLinkedList = new LinkedList<DriverTrip>(addedTrips);
                    LinkedListNode<DriverTrip> addedTripAsNode = addedTripsAsLinkedList.Find(addedTrip);

                    DriverTrip? previousTrip = addedTripAsNode.Previous?.Value;
                    DriverTrip? nextTrip = addedTripAsNode.Next?.Value;

                    try
                    {
                        if (previousTrip != null)
                        {
                            if (addedTripAsNode.Value.BeginTime <= previousTrip.EndTime)
                            {
                                // Invalid
                                throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                    $"Bạn đang chọn chuyến đi có thời gian bắt đầu ({addedTripAsNode.Value.BeginTime}) " +
                                    $"sớm hơn so với thời gian dự kiến bạn sẽ kết thúc một chuyến đi bạn đã chọn trước đó ({previousTrip.EndTime})");
                            }
                        }

                        if (nextTrip != null)
                        {
                            // Has Next Trip
                            if (addedTripAsNode.Value.EndTime >= nextTrip.BeginTime)
                            {
                                throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                    $"Bạn đang chọn chuyến đi có thời gian kết thúc dự kiến ({addedTripAsNode.Value.EndTime}) " +
                                    $"trễ hơn so với thời gian bạn phải bắt đầu một chuyến đi bạn đã chọn trước đó ({nextTrip.BeginTime})");
                            }
                        }

                        // No exception thrown, driver is available
                        availableDrivers = availableDrivers.Append(new UserViewModel(driver));

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Invalid Driver Schedules: {0}", ex.GeneratorErrorMessage());
                    }
                    
                }
            }

            return availableDrivers;
        }

        #region Private
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

        private async Task<DriverTripsOfDate> GetDriverSchedulesInDateAsync(Guid driverId,
            DateTime date,
            CancellationToken cancellationToken)
        {
            // Get driver current schedules
            //IList<DriverTripsOfDate> driverTrips = new List<DriverTripsOfDate>();
            //DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);

            IEnumerable<BookingDetail> driverBookingDetails = await
                work.BookingDetails.GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                        bd.DriverId.Value.Equals(driverId)
                        && bd.Status != BookingDetailStatus.CANCELLED
                        && bd.Date == date), cancellationToken: cancellationToken);

            if (driverBookingDetails.Any())
            {
                IEnumerable<Guid> driverBookingIds = driverBookingDetails.Select(d => d.BookingId).Distinct();
                IEnumerable<Booking> driverBookings = await work.Bookings.GetAllAsync(
                    query => query.Where(b => driverBookingIds.Contains(b.Id)), cancellationToken: cancellationToken);

                IEnumerable<Guid> driverStationIds = driverBookingDetails.Select(
                    b => b.StartStationId).Concat(driverBookingDetails.Select(b => b.EndStationId))
                    .Distinct();
                IEnumerable<Station> driverStations = await work.Stations.GetAllAsync(
                    query => query.Where(s => driverStationIds.Contains(s.Id)), includeDeleted: true,
                    cancellationToken: cancellationToken);

                IEnumerable<DriverTrip> trips = from detail in driverBookingDetails
                                                join booking in driverBookings
                                                    on detail.BookingId equals booking.Id
                                                join startStation in driverStations
                                                    on detail.StartStationId equals startStation.Id
                                                join endStation in driverStations
                                                    on detail.EndStationId equals endStation.Id
                                                select new DriverTrip()
                                                {
                                                    Id = detail.Id,
                                                    BeginTime = detail.CustomerDesiredPickupTime,
                                                    EndTime = DateTimeUtilities.CalculateTripEndTime(detail.CustomerDesiredPickupTime, booking.Duration),
                                                    StartLocation = new GoogleMapPoint()
                                                    {
                                                        Latitude = startStation.Latitude,
                                                        Longitude = startStation.Longitude
                                                    },
                                                    EndLocation = new GoogleMapPoint()
                                                    {
                                                        Latitude = endStation.Latitude,
                                                        Longitude = endStation.Longitude
                                                    }
                                                };

                trips = trips.OrderBy(t => t.BeginTime);

                DriverTripsOfDate tripsOfDate = new DriverTripsOfDate
                {
                    Date = DateOnly.FromDateTime(date),
                    Trips = trips.ToList()
                };
                return tripsOfDate;
                   }
            return new DriverTripsOfDate
            {
                Date = DateOnly.FromDateTime(date),
                Trips = new List<DriverTrip>()
            };

        }
        #endregion

    }
}
