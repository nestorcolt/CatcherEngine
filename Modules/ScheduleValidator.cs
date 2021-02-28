using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    class ScheduleValidator
    {
        public Dictionary<int, List<long>> ScheduleSlots = new Dictionary<int, List<long>>();
        private readonly string _timeZone;
        private int _scheduleSlotCounter;
        private int _daysToValidate = 7;

        public ScheduleValidator(JToken weekSchedule, string timeZone)
        {
            _timeZone = timeZone;
            CreateWeekMap(weekSchedule);
        }

        private void CreateWeekMap(JToken weekSchedule)
        {
            DateTime today = SetTimeZone(DateTime.Today, _timeZone);
            Console.WriteLine($"{today.ToLongDateString()} {today.ToLongTimeString()}");
            List<dynamic> dateObjects = new List<dynamic>() { today };

            for (int i = 1; i < _daysToValidate; i++)
            {
                DateTime day = today.AddDays(i);
                dateObjects.Add(day);
            }

            foreach (var dateObject in dateObjects)
            {
                foreach (var daySchedule in weekSchedule)
                {
                    if ((int)daySchedule["dayOfWeek"] == (int)dateObject.DayOfWeek)
                    {
                        MapDaySchedule(dateObject, daySchedule["times"]);
                    }
                }
            }
        }

        public int GetTimestamp(DateTime customDate)
        {
            TimeSpan time = (customDate - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        public DateTime SetTimeZone(DateTime timeToConvert, string timeZone)
        {
            // Convert the time zone in UNIX format given Datetime object
            TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime targetTime = TimeZoneInfo.ConvertTime(timeToConvert, est);
            return targetTime;
        }

        private void MapDaySchedule(dynamic date, JToken daySchedule)
        {
            foreach (var day in daySchedule)
            {
                var startTime = day["start"].ToString().Split(":");
                var endTime = day["end"].ToString().Split(":");

                DateTime startDateTime = new DateTime(date.Year, date.Month, date.Day, int.Parse(startTime[0]), int.Parse(startTime[1]), 0);
                DateTime endDateTime = new DateTime(date.Year, date.Month, date.Day, int.Parse(endTime[0]), int.Parse(endTime[1]), 0);

                long startUnixDate = GetTimestamp(startDateTime);
                long stopUnixDate = GetTimestamp(endDateTime);

                ScheduleSlots[_scheduleSlotCounter] = new List<long>() { startUnixDate, stopUnixDate };
                _scheduleSlotCounter++;
            }
        }

        public bool ValidateSchedule(long blockTime)
        {
            for (int i = 0; i < _scheduleSlotCounter; ++i)
            {
                long start = ScheduleSlots[i][0];
                long stop = ScheduleSlots[i][1];

                if (blockTime >= start && blockTime <= stop)
                    return true;
            }

            return false;
        }
    }
}
