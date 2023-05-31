using ViGo.Domain.Enumerations;

namespace ViGo.API.SignalR.Core
{
    public interface ISignalRService
    {
        Task SendAllAsync(string method, object obj);
        Task SendToUserAsync(Guid userId, string method, object obj);
        Task SendToUsersAsync(List<Guid> userIds, string method, object obj);
        Task SendToUsersGroupRoleNameAsync(UserRole role, string method, object obj);
        Task SendToGroupAsync(string groupName, string method, object obj);
    }
}
