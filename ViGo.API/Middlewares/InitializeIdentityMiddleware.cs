using ViGo.Utilities;

namespace ViGo.API.Middlewares
{
    public class InitializeIdentityMiddleware
    {
        private readonly RequestDelegate _next;

        public InitializeIdentityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //if (context.User != null &&
            //    context.User.Identity.IsAuthenticated)
            //{
            IdentityUtilities.Initialize(context.User);
            //}
            await _next(context);
        }
    }
}
