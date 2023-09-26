using System.Globalization;
using System.Text.RegularExpressions;

namespace ViGo.Utilities
{
    public static class StringUtilities
    {
        public static string ToTitleCase(this string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }

        public static string TransformToTitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());

        }

        public static string VndFormat(this double number)
        {
            Regex regex = new Regex("/(\\d)(?=(\\d{3})+(?!\\d))/g");
            return regex.Replace(number.ToString(),"$1,");
        }
    }
}
