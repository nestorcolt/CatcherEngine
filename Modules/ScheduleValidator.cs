using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    class ScheduleValidator
    {
        public Dictionary<int, List<long>> ScheduleSlots = new Dictionary<int, List<long>>();
        private int _scheduleSlotCounter;

        public ScheduleValidator(JToken weekSchedule)
        {
            CreateWeekMap(weekSchedule);
        }

        public int GetTimestamp(DateTime customDate)
        {
            TimeSpan time = (customDate - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private void CreateWeekMap(JToken weekSchedule)
        {
            DateTime today = DateTime.Today;
            DateTime plus1Day = today.AddDays(1);
            DateTime plus2Day = today.AddDays(2);

            List<dynamic> dateObjects = new List<dynamic>() { today, plus1Day, plus2Day };

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
