using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.UserLicenses;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;

namespace ViGo.Services
{
    public class UserLicenseServices : BaseServices
    {
        public UserLicenseServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<UserLicenseViewModel>> GetAllUserLicenses(
            PaginationParameter pagination, UserLicenseSortingParameters sorting,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<UserLicense> userLicenses = await work.UserLicenses.GetAllAsync(cancellationToken: cancellationToken);

            userLicenses = userLicenses.Sort(sorting.OrderBy);

            int totalRecords = userLicenses.Count();
            userLicenses = userLicenses.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> userIds = userLicenses.Select(id => id.UserId);
            IEnumerable<User> users = await work.Users.GetAllAsync(
                q => q.Where(x => userIds.Contains(x.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserViewModel> userViews = from user in users
                                                   select new UserViewModel(user);

            IEnumerable<UserLicenseViewModel> listUserLicense = from userLicense in userLicenses
                                                                join userView in userViews
                                                                    on userLicense.UserId equals userView.Id
                                                                select new UserLicenseViewModel(userLicense, userView);

            return listUserLicense.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<IEnumerable<UserLicenseViewModel>> GetAllUserLicensesByUserID(Guid userId, 
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                userId = IdentityUtilities.GetCurrentUserId();
            }

            IEnumerable<UserLicense> userLicenses = await work.UserLicenses.GetAllAsync(
                q => q.Where(x => x.UserId.Equals(userId)), cancellationToken: cancellationToken);

            userLicenses = userLicenses.OrderBy(u => u.Status);

            //IEnumerable<Guid> userIds = userLicenses.Select(q => q.UserId);

            //IEnumerable<User> users = await work.Users.GetAllAsync(
            //    q => q.Where(x => userIds.Contains(x.Id)),cancellationToken : cancellationToken);
            User? user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            if (user is null || user.Role != UserRole.DRIVER)
            {
                if (user != null && user.Id.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!!");
                }

                throw new ApplicationException("Người dùng không hợp lệ!!");
            }

            //IEnumerable<UserViewModel> userViews = from user in users
            //                                       select new UserViewModel(user);

            UserViewModel userModel = new UserViewModel(user);

            IEnumerable<UserLicenseViewModel> listUserLicense = from userLicense in userLicenses
                                                                    //join userView in userViews
                                                                    //on userLicense.UserId equals userView.Id
                                                                select new UserLicenseViewModel(userLicense, userModel);

            return listUserLicense;

        }

        public async Task<UserLicenseViewModel> GetUserLicenseByID(Guid id, CancellationToken cancellationToken)
        {
            UserLicense userLicense = await work.UserLicenses.GetAsync(id, cancellationToken: cancellationToken);
            Guid userId = userLicense.UserId;
            User user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);
            UserLicenseViewModel userLicenseView = new UserLicenseViewModel(userLicense, userViewModel);
            return userLicenseView;

        }

        public async Task<UserLicenseViewModel> CreateUserLicense(UserLicenseCreateModel userLicense, CancellationToken cancellationToken)
        {
            //check current User
            //if (!userLicense.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
            //{
            //    throw new ApplicationException("Vai trò của bạn không được phép thực hiện chức năng này!");
            //}
            UserLicense newUserLicense = new UserLicense
            {
                //UserId = userLicense.UserId,
                UserId = IdentityUtilities.GetCurrentUserId(),
                FrontSideFile = userLicense.FrontSideFile,
                BackSideFile = userLicense.BackSideFile,
                LicenseType = userLicense.LicenseType,
                Status = UserLicenseStatus.PENDING,
                IsDeleted = false,

            };
            await work.UserLicenses.InsertAsync(newUserLicense, cancellationToken: cancellationToken);
            var result = await work.SaveChangesAsync(cancellationToken: cancellationToken);

            User user = await work.Users.GetAsync(newUserLicense.UserId, cancellationToken: cancellationToken);
            UserViewModel newUserViewModel = new UserViewModel(user);
            UserLicenseViewModel userLicenseView = new UserLicenseViewModel(newUserLicense, newUserViewModel);
            if (result > 0)
            {
                return userLicenseView;
            }
            return null!;

        }

        public async Task<UserLicenseViewModel> UpdateUserLicense(Guid id,
            UserLicenseUpdateModel userLicenseUpdate, CancellationToken cancellationToken)
        {
            var currentUserLicense = await work.UserLicenses.GetAsync(id,
                cancellationToken: cancellationToken);

            if (currentUserLicense is null)
            {
                throw new ApplicationException("Thông tin bằng cấp không hợp lệ!!");
            }

            if (userLicenseUpdate.Status != null) currentUserLicense.Status = userLicenseUpdate.Status;
            if (userLicenseUpdate.IsDeleted != null) currentUserLicense.IsDeleted = (bool)userLicenseUpdate.IsDeleted;

            await work.UserLicenses.UpdateAsync(currentUserLicense!);
            await work.SaveChangesAsync(cancellationToken);

            User user = await work.Users.GetAsync(currentUserLicense.UserId,
                cancellationToken: cancellationToken);

            UserViewModel newUserViewModel = new UserViewModel(user);
            UserLicenseViewModel userLicenseView = new UserLicenseViewModel(currentUserLicense, newUserViewModel);

            return userLicenseView;

        }
    }
}
