using System;

namespace SearchEngine
{
    class Program

    {
        static void Main(string[] args)
        {
            string zone = "Pacific Standard Time";
            Console.WriteLine(UnixToDateTime(1615226400, zone).ToLongTimeString());
            Console.WriteLine(UnixToDateTime(1615233600, zone).ToLongTimeString());
        }

        public static DateTime SetTimeZone(DateTime timeToConvert, string timeZone)
        {
            TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime targetTime = TimeZoneInfo.ConvertTime(timeToConvert, est);
            return targetTime;
        }

        private static DateTime UnixToDateTime(long timeInSeconds, string timeZone)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timeInSeconds);
            DateTime dateTime = dateTimeOffset.DateTime;
            return SetTimeZone(dateTime, timeZone);
        }
    }
}
