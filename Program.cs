using System;

namespace SearchEngine
{
    class Program

    {
        static void Main(string[] args)
        {

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