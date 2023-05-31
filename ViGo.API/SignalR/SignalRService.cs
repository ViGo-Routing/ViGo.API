using Microsoft.AspNetCore.SignalR;
using ViGo.API.SignalR.Core;
using ViGo.Domain.Enumerations;

namespace ViGo.API.SignalR
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<SignalRHub> signalRHub;

        public SignalRService(IHubContext<SignalRHub> signalRHub)
        {
            this.signalRHub = signalRHub;
        }

        public async Task SendAllAsync(string method, object obj)
        {
            await signalRHub.Clients.All.SendAsync(method, obj);
        }

        public async Task SendToGroupAsync(string groupName, string method, object obj)
        {
            await signalRHub.Clients.Group(groupName).SendAsync(method, obj);
        }

        public async Task SendToUserAsync(Guid userId, string method, object obj)
        {
            await signalRHub.Clients.User(userId.ToString()).SendAsync(method, obj);
        }

        public async Task SendToUsersAsync(List<Guid> userIds, string method, object obj)
        {
            await signalRHub.Clients.Users(userIds.Select(u => u.ToString())).SendAsync(method, obj);
        }

        public async Task SendToUsersGroupRoleNameAsync(UserRole role, string method, object obj)
        {
            await signalRHub.Clients.Group(role.ToString()).SendAsync(method, obj);
        }
    }
}
