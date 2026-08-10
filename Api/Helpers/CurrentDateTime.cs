using Azure.Core;
using System.Text.RegularExpressions;

namespace QuasarApi.Helpers
{
    public class CurrentDateTime
    {
        public static DateTime GetCurrentDateTime()
        {
            DateTime result;

            try
            {
                DateTime utc = DateTime.UtcNow;
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                result = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            }
            catch (Exception)
            {
                result = DateTime.Now;
            }

            return result;
        }
    }
}
