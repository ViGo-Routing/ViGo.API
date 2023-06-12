using System.Net;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Extensions;

namespace ViGo.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            CancellationToken cancellationToken =
                httpContext.RequestAborted;
            try
            {
                await _next(httpContext);
            }
            catch (AccessDeniedException ex)
            {
                _logger.LogError($"AccessDenied Exception: {ex.GeneratorErrorMessage()}");
                await HandleExceptionAsync(HttpStatusCode.Forbidden, 
                    httpContext, ex.GeneratorErrorMessage(), cancellationToken);
                //return StatusCode(403, ex.GeneratorErrorMessage());
            }
            catch (ApplicationException appEx)
            {
                _logger.LogError($"Application Exception: {appEx.GeneratorErrorMessage()}");
                await HandleExceptionAsync(HttpStatusCode.BadRequest, 
                    httpContext, appEx.GeneratorErrorMessage(), cancellationToken);
                //return StatusCode(400, appEx.GeneratorErrorMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.GeneratorErrorMessage()}");
                await HandleExceptionAsync(HttpStatusCode.InternalServerError, 
                    httpContext, ex.GeneratorErrorMessage(), cancellationToken);
                //return StatusCode(500, ex.GeneratorErrorMessage());
            }
        }

        private async Task HandleExceptionAsync(HttpStatusCode statusCode,
            HttpContext context, string message, 
            CancellationToken cancellationToken = default)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsync(message, cancellationToken);
        }
    }
}
