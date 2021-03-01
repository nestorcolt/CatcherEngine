using System;

namespace tester
{
    class Program
    {
        static void Main(string[] args)
        {
            // Block time from Flex
            // 1614558600 GMT + 1 
            // Sunday, 28 Feb 2021 18:30:00

            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(1614558600);
            DateTime dateTime = dateTimeOffset.DateTime;
            TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime blockTime = TimeZoneInfo.ConvertTime(dateTime, est);

            Console.WriteLine($"Block User Time {blockTime.ToLongDateString()} {blockTime.ToLongTimeString()}");

            DateTime startDateTime = new DateTime(2021, 02, 28, 12, 30, 00);
            DateTime endDateTime = new DateTime(2021, 02, 28, 18, 30, 00);

            if (startDateTime <= blockTime && blockTime <= endDateTime)
            {
                Console.WriteLine("Offer Accepted");
            }

            //TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            ////DateTime targetTime = TimeZoneInfo.ConvertTime(startDateTime, est);

            //Int32 unixTimestamp = (Int32)(startDateTime.Subtract(DateTime.UnixEpoch)).TotalSeconds;
            //Console.WriteLine($"Seconds: {unixTimestamp}");
            //Console.WriteLine($"{startDateTime.ToLongDateString()} {startDateTime.ToLongTimeString()}");
        }
    }
}