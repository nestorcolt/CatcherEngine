using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    class OfferGenerator
    {
        private readonly JObject _offerModel;
        private readonly int _start;
        private int _stop;

        public OfferGenerator()
        {
            _offerModel = LoadJsonAsync("Modules/offers.json").Result;
            DateTime today = DateTime.Today;

            _start = GetTimestamp(today);
            _stop = GetTimestamp(today.AddDays(7));
        }

        public async Task<JObject> LoadJsonAsync(string filePath)
        {
            using StreamReader jFiler = new StreamReader(filePath);
            JObject jsonTemplate = JObject.Parse(await jFiler.ReadToEndAsync());
            return jsonTemplate;
        }

        public int GetTimestamp(DateTime customDate)
        {
            TimeSpan time = (customDate - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        public void GetTimeFromSeconds(long seconds)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);
            DateTime dateTime = dateTimeOffset.UtcDateTime;
            string blockTime = $"Offer: {dateTime.DayOfWeek} at {dateTime.ToString("HH:mm")}";
            Console.WriteLine(blockTime);
        }

        public List<JToken> GenerateOffers()
        {
            var offers = new List<JToken>();

            for (int i = 0; i < 5; i++) // number of offers returned
            {
                Random rnd = new Random();
                long unixTargetTime = rnd.Next(_start, _stop);
                JToken offerDict = (JToken)_offerModel.DeepClone();

                offerDict["startTime"] = unixTargetTime.ToString();
                offers.Add(offerDict);
            }

            return offers;
        }
    }
}