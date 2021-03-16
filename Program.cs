using SearchEngine.Modules;
using System;

namespace SearchEngine
{
    class Program

    {
        static void Main(string[] args)
        {
            string rt = "Atnr|EwICIAT6bN6fu3CAQv8IrNihJ-gdEUgyuCF8eJLx1wp4ZDk8Na7a8HGb4PGEte0VEQ1tCHaUnKPXyQn2CH03yV9o0YptXQx6dAt2CpZKy90gX0160--YtwiTtjVCoOQGcaP2R0le6nq9gMer2QeFQjrcL_WMgjlAcZTzfSlO_VlA_SUL4B2Jb_nDFWYqGmMeY7I1xTP-81z2oCZlr5c-YN_vh08VMcXtxRdwMegRfzCHoPTlqLq148aLzh0LBKbsbQ1d9YN6LYWv5S8i6C13YSVbHq75";
            Authenticator.Authenticate(rt, "5").Wait();
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