using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Utilities.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GeneratorErrorMessage (
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
