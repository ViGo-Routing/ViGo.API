namespace ViGo.Utilities.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GeneratorErrorMessage(
            this Exception exception)
        {
            string message = exception.Message;
            if (exception.InnerException != null)
            {
                message += "\nDetails: " + exception.InnerException.Message;
            }

            return message;
        }
    }
}
