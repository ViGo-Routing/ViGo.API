using System;
using System.Text.RegularExpressions;

namespace ViGo.Utilities.Validator
{
    public static class StringValidator
    {
        /// <summary>
        /// Validate a string
        /// </summary>
        /// <param name="stringToValidate">String to Validate</param>
        /// <param name="allowEmpty">True if the string is allowed to be empty. Default is False</param>
        /// <param name="emptyErrorMessage">Error message to be thrown when the string is empty (only if allowEmpty is True)</param>
        /// <param name="minLength">Minimum length of the string. Default is -1, which means that the string is not required to be validated in the minimum length</param>
        /// <param name="minLengthErrorMessage">Error message to be thrown when the string does not meet the minimum length (only if minLength > 0)</param>
        /// <param name="maxLength">Maximum length of the string, Default is -1, which means that the string is not required to be validated in the maximum length</param>
        /// <param name="maxLengthErrorMessage">Error message to be thrown when the string does not meet the maximum length (only if maxLength > 0)</param>
        /// <returns>True if the string meets all the validation, otherwise, throw Exception with message of the corresponding message</returns>
        /// <exception cref="ApplicationException">Throw exception with the error message if the string is not valid</exception>
        public static bool StringValidate(this string stringToValidate,
            bool allowEmpty = false, string emptyErrorMessage = "",
            int minLength = -1, string minLengthErrorMessage = "",
            int maxLength = -1, string maxLengthErrorMessage = "")
        {
            if (!allowEmpty)
            {
                // allowEmpty = false
                if (string.IsNullOrEmpty(stringToValidate))
                {
                    throw new ApplicationException(emptyErrorMessage);
                }
                if (minLength > 0)
                {
                    if (stringToValidate.Length < minLength)
                    {
                        throw new ApplicationException(minLengthErrorMessage);
                    }
                }
                if (maxLength > 0)
                {
                    if (stringToValidate.Length > maxLength)
                    {
                        throw new ApplicationException(maxLengthErrorMessage);
                    }
                }
            }
            else
            {
                // allowEmpty = true
                if (!string.IsNullOrEmpty(stringToValidate))
                {
                    if (minLength > 0)
                    {
                        //if (stringToValidate == null)
                        //{
                        //    throw new ArgumentNullException("Invalid string input!!!");
                        //}
                        if (stringToValidate.Length < minLength)
                        {
                            throw new ApplicationException(minLengthErrorMessage);
                        }
                    }
                    if (maxLength > 0)
                    {
                        //if (stringToValidate == null)
                        //{
                        //    throw new ArgumentNullException("Invalid string input!!!");
                        //}
                        if (stringToValidate.Length > maxLength)
                        {
                            throw new ApplicationException(maxLengthErrorMessage);
                        }
                    }
                }

            }
            return true;
        }

        /// <summary>
        /// Validate an email
        /// </summary>
        /// <param name="stringToCheck">Email string to check</param>
        /// <param name="errorMessage">Error Message to display if the string is not an valid email</param>
        /// <returns>True if the string is an valid email</returns>
        /// <exception cref="ApplicationException">Throw exception with the error message if the string is not an email</exception>
        public static bool IsEmail(this string stringToCheck, string errorMessage)
        {
            //EmailAddressAttribute emailAddressAttribute = new EmailAddressAttribute();
            //if (!emailAddressAttribute.IsValid(stringToCheck))
            //{
            //    throw new ApplicationException(errorMessage);
            //}

            var re = @"^(([^<>()[\]\.,;:\s@\""]+(\.[^<>()[\]\.,;:\s@\""]+)*)|(\"".+\""))@(([^<>()[\]\.,;:\s@\""]+\.)+[^<>()[\]\.,;:\s@\""]{2,})$";
            Regex emailReg = new Regex(re);
            if (!emailReg.IsMatch(stringToCheck))
            {
                throw new ApplicationException(errorMessage);
            }
            return true;
        }

        public static bool IsPhoneNumber(this string stringToCheck, string errorMessage)
        {
            var re = @"^\+(?:[0-9]●?){6,14}[0-9]$";
            Regex phoneReg = new Regex(re);
            if (!phoneReg.IsMatch(stringToCheck))
            {
                throw new ApplicationException(errorMessage);
            }
            return true;
        }
    }
}
