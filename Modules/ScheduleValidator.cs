﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    class ScheduleValidator
    {
        public Dictionary<int, List<DateTime>> ScheduleSlots = new Dictionary<int, List<DateTime>>();
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

        public DateTime SetTimeZone(DateTime timeToConvert, string timeZone)
        {
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

                ScheduleSlots[_scheduleSlotCounter] = new List<DateTime>() { startDateTime, endDateTime };
                _scheduleSlotCounter++;
            }
        }

        private DateTime UnixToDateTime(long timeInSeconds)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timeInSeconds);
            DateTime dateTime = dateTimeOffset.DateTime;
            return SetTimeZone(dateTime, _timeZone);
        }

        public bool ValidateSchedule(long blockTime)
        {
            DateTime blockDateTime = UnixToDateTime(blockTime);

            for (int i = 0; i < _scheduleSlotCounter; ++i)
            {
                DateTime start = ScheduleSlots[i][0];
                DateTime stop = ScheduleSlots[i][1];

                Console.WriteLine($"Check: {start.ToLongDateString()} {start.ToLongTimeString()} | " +
                                  $"{blockDateTime.ToLongDateString()} {blockDateTime.ToLongTimeString()} | " +
                                  $"{stop.ToLongDateString()} {stop.ToLongTimeString()}");

                if (start <= blockDateTime && blockDateTime <= stop)
                    return true;
            }

            return false;
        }
    }
}
