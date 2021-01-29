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
            _stop = GetTimestamp(today.AddDays(2));
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

        public List<JToken> GenerateOffers()
        {
            var offers = new List<JToken>();
            Random rnd = new Random();

            for (int i = 0; i < 10; i++)
            {
                int unixTargetTime = rnd.Next(_start, _stop);
                _offerModel["startTime"] = unixTargetTime.ToString();
                offers.Add(_offerModel);
            }

            return offers;
        }
    }
}