using System;

namespace ViGo.Utilities.Validator
{
    public static class DateTimeValidator
    {
        /// <summary>
        /// Check if a date with time is between 2 specific datetime values
        /// </summary>
        /// <param name="dateToCompare">Datetime value to check</param>
        /// <param name="minDate">Minimum datetime</param>
        /// <param name="maxDate">Maximum datetime</param>
        /// <returns>True if the date is between 2 specific datetime values; Otherwise, false</returns>
        public static bool IsDateBetween(this DateTime dateToCompare,
            DateTime minDate, DateTime maxDate)
        {
            return dateToCompare >= minDate && dateToCompare <= maxDate;
        }

        /// <summary>
        /// Validate a datetime
        /// </summary>
        /// <param name="dateToValidate">The datetime to validate</param>
        /// <param name="minimum">Minimum allowed value</param>
        /// <param name="minErrorMessage">Error Message if the datetime is earlier than minimum</param>
        /// <param name="maximum">Maximum allowed value</param>
        /// <param name="maxErrorMessage">Error Message if the datetime is after than the maximum</param>
        /// <returns>True if the datetime is valid</returns>
        /// <exception cref="ApplicationException">Throw exception with the error message if the datetime is not valid</exception>
        public static bool DateTimeValidate(this DateTime dateToValidate,
            DateTime? minimum = null, string minErrorMessage = "",
            DateTime? maximum = null, string maxErrorMessage = "")
        {
            if (minimum.HasValue)
            {
                if (dateToValidate < minimum.Value)
                {
                    throw new ApplicationException(minErrorMessage);
                }
            }
            if (maximum.HasValue)
            {
                if (dateToValidate > maximum.Value)
                {
                    throw new ApplicationException(maxErrorMessage);
                }
            }
            return true;
        }
    }
}
