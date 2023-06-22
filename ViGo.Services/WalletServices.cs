using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Users;
using ViGo.Models.Wallets;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class WalletServices : BaseServices
    {
        public WalletServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }


        //public async Task<Wallet> CreateWalletAsync(Guid userId)
        //{
        //    Wallet wallet = new Wallet
        //    {
        //        Id = Guid.NewGuid(),
        //        UserId = userId,
        //        Balance = 0,
        //        Type = WalletType.SYSTEM,
        //        Status = WalletStatus.ACTIVE,
        //        CreatedBy = userId,
        //        UpdatedBy = userId,
        //        IsDeleted = false,
        //    };

        //    await work.Wallets.InsertAsync(wallet);
        //    await work.SaveChangesAsync();
        //    return wallet;
        //}

        public async Task<IPagedEnumerable<WalletViewModel>> GetAllWalletsAsync(PaginationParameter pagination, HttpContext context)
        {
            //IEnumerable<Wallet> wallets = await work.Wallets.GetAllAsync(q => q.Where(
            //    items => items.Type.Equals(WalletType.PERSONAL)));

            IEnumerable<Wallet> wallets = await work.Wallets.GetAllAsync();
            int totalRecords = wallets.Count();
            wallets = wallets.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;
            
            IEnumerable<Guid> userIds = wallets.Select(w => w.UserId);
            IEnumerable<User> users = await work.Users.GetAllAsync(e => e.Where(r => userIds.Contains(r.Id)));
            IEnumerable<UserViewModel> userView = from user in users
                                                  select new UserViewModel(user);
            IEnumerable<WalletViewModel> listWallet = from wallet in wallets
                                                      join cus in userView
                                                        on wallet.UserId equals cus.Id
                                                      select new WalletViewModel(wallet, cus);

            return listWallet.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize, totalRecords, context);
        }
        public async Task<WalletViewModel> GetWalletByUserId(Guid userId)
        {
            Wallet wallet = await work.Wallets.GetAsync(q => q.UserId.Equals(userId));
            Guid userID = wallet.UserId;
            User user = await work.Users.GetAsync(userID);
            UserViewModel userView = new UserViewModel(user);
            WalletViewModel walletView = new WalletViewModel(wallet, userView);

            return walletView;
        }

        public async Task<WalletViewModel> UpdateWalletStatusById(Guid id, WalletUpdateModel walletUpdate)
        {
            var currentWallet = await work.Wallets.GetAsync(id);

            currentWallet.Status = walletUpdate.Status;

            await work.Wallets.UpdateAsync(currentWallet);
            await work.SaveChangesAsync();

            Guid userID = currentWallet.UserId;
            User user = await work.Users.GetAsync(userID);
            UserViewModel userView = new UserViewModel(user);
            WalletViewModel walletView = new WalletViewModel(currentWallet, userView);
            return walletView;
        }
    }
}
