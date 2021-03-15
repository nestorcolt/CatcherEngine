using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;
using SearchEngine.Serverless;

namespace SearchEngine
{
    class Program

    {
        static void Main(string[] args)
        {
            string version = settings.Default.FlexAppVersion;
            Console.WriteLine(version.Replace(".", ""));
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