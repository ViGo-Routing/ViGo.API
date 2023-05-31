using Microsoft.AspNetCore.SignalR;

namespace ViGo.API.SignalR
{
    public class SignalRHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
