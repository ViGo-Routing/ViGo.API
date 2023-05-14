using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class UserServices : BaseServices<User>
    {
        public UserServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            IEnumerable<User> users
                = await work.Users.GetAllAsync();

            return users;
        }
    }
}
