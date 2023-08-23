namespace ViGo.Utilities.Validator
{
    public static class NumberValidator
    {
        /// <summary>
        /// Validate an integer value
        /// </summary>
        /// <param name="integerToValidate">The integer number to validate</param>
        /// <param name="minimum">Minimum allowed value</param>
        /// <param name="minErrorMessage">Error Message if the number is smaller than the minimum</param>
        /// <param name="maximum">Maximum allowed value</param>
        /// <param name="maxErrorMessage">Error Message if the number is bigger than the maximum</param>
        /// <returns>True if the integer number is valid</returns>
        /// <exception cref="ApplicationException">Throw exception with the error message if the integer number is not valid</exception>
        public static bool IntegerValidate(this int integerToValidate,
            int? minimum = null, string minErrorMessage = "",
            int? maximum = null, string maxErrorMessage = "")
        {
            if (minimum.HasValue)
            {
                if (integerToValidate < minimum.Value)
                {
                    throw new ApplicationException(minErrorMessage);
                }
            }
            if (maximum.HasValue)
            {
                if (integerToValidate > maximum.Value)
                {
                    throw new ApplicationException(maxErrorMessage);
                }
            }
            return true;
        }

        /// <summary>
        /// Validate a decimal number
        /// </summary>
        /// <param name="numberToValidate">The decimal number to validate</param>
        /// <param name="minimum">Minimum allowed value</param>
        /// <param name="minErrorMessage">Error Message if the number is smaller than minimum</param>
        /// <param name="maximum">Maximum allowed value</param>
        /// <param name="maxErrorMessage">Error Message if the number is bigger than the maximum</param>
        /// <returns>True if the decimal number is valid</returns>
        /// <exception cref="ApplicationException">Throw exception with the error message if the decimal number is not valid</exception>
        public static bool DecimalValidate(this decimal numberToValidate,
            decimal? minimum = null, string minErrorMessage = "",
            decimal? maximum = null, string maxErrorMessage = "")
        {
            if (minimum.HasValue)
            {
                if (numberToValidate < minimum.Value)
                {
                    throw new ApplicationException(minErrorMessage);
                }
            }
            if (maximum.HasValue)
            {
                if (numberToValidate > maximum.Value)
                {
                    throw new ApplicationException(maxErrorMessage);
                }
            }
            return true;
        }

        /// <summary>
        /// Validate a double number
        /// </summary>
        /// <param name="numberToValidate">The double number to validate</param>
        /// <param name="minimum">Minimum allowed value</param>
        /// <param name="minErrorMessage">Error Message if the number is smaller than minimum</param>
        /// <param name="maximum">Maximum allowed value</param>
        /// <param name="maxErrorMessage">Error Message if the number is bigger than the maximum</param>
        /// <returns>True if the double number is valid</returns>
        /// <exception cref="ApplicationException">Throw exception with the error message if the double number is not valid</exception>
        public static bool DoubleValidate(this double numberToValidate,
            double? minimum = null, string minErrorMessage = "",
            double? maximum = null, string maxErrorMessage = "")
        {
            if (minimum.HasValue)
            {
                if (numberToValidate < minimum.Value)
                {
                    throw new ApplicationException(minErrorMessage);
                }
            }
            if (maximum.HasValue)
            {
                if (numberToValidate > maximum.Value)
                {
                    throw new ApplicationException(maxErrorMessage);
                }
            }
            return true;
        }
    }
}
