using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;
using SearchEngine.Properties;

namespace SearchEngine
{
    class BlockCatcher : Engine
    {
        private readonly Dictionary<string, object> _statsDict = new Dictionary<string, object>();
        private readonly SignatureObject _signature = new SignatureObject();
        private readonly Stopwatch _mainTimer = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.Now;

        public BlockCatcher()
        {
            // setup engine details
            InitializeEngine();

        }

        private async Task<HttpStatusCode> GetOffersAsyncHandle()
        {
            SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");

            ApiHelper.AddRequestHeaders(RequestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(RequestDataHeadersDictionary, ApiHelper.CatcherClient);

            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, ServiceAreaFilterData, ApiHelper.SeekerClient);
            TotalApiCalls++;

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && offerList.HasValues)
                {
                    Thread acceptThread = new Thread(task => AcceptOffers(offerList));
                    acceptThread.Start();

                    TotalOffersCounter += offerList.Count();
                }
            }

            return response.StatusCode;
        }

        private void SignRequestHeaders(string url)
        {
            SortedDictionary<string, string> signatureHeaders = _signature.CreateSignature(url, AccessToken);

            RequestDataHeadersDictionary["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
            RequestDataHeadersDictionary["X-Flex-Client-Time"] = GetTimestamp().ToString();
            RequestDataHeadersDictionary["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
            RequestDataHeadersDictionary["Authorization"] = signatureHeaders["Authorization"];
        }

        private bool ValidateArea(string serviceAreaId)
        {
            if (Areas.Count == 0)
                return true;

            if (Areas.Contains(serviceAreaId))
                return true;

            return false;
        }

        public async Task AcceptSingleOfferAsync(JToken block)
        {
            long offerTime = (long)block["startTime"];
            string serviceAreaId = (string)block["serviceAreaId"];
            float offerPrice = (float)block["rateInfo"]["priceAmount"];

            // Validates the calendar schedule for this user
            bool scheduleValidation = ScheduleValidator.ValidateSchedule(offerTime);
            Console.WriteLine($"Schedule validated: {scheduleValidation}");

            bool areaValidation = ValidateArea(serviceAreaId);
            Console.WriteLine($"Area validated: {areaValidation}");

            if (scheduleValidation && offerPrice >= MinimumPrice && areaValidation)
            {
                string offerId = block["offerId"].ToString();
                Console.WriteLine("All validations passed!!!");

                var acceptHeader = new Dictionary<string, string>
                {
                    {"__type", $"AcceptOfferInput:{ApiHelper.AcceptInputUrl}"},
                    {"offerId", offerId}
                };

                string jsonData = JsonConvert.SerializeObject(acceptHeader);
                //HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.AcceptUri, jsonData, ApiHelper.CatcherClient);
                HttpResponseMessage response = new HttpResponseMessage();


                if (response.IsSuccessStatusCode)
                {
                    // send to owner endpoint accept data to log and send to the user the notification
                    TotalAcceptedOffers++;
                }

                Console.WriteLine($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n");
            }
        }

        public void AcceptOffers(JToken offerList)
        {
            Parallel.For(0, offerList.Count(), n =>
            {
                JToken innerBlock = offerList[n];
                Thread accept = new Thread(async task => await AcceptSingleOfferAsync(innerBlock));
                accept.Start();
            });
        }

        public void LookingForBlocksLegacy()
        {
            Stopwatch watcher = Stopwatch.StartNew();
            Console.WriteLine("Search Loop Status: ON");

            while (true)

            {
                // start logic here main request
                HttpStatusCode statusCode = GetOffersAsyncHandle().Result;

                // custom delay to save request
                Thread.Sleep((int)Speed);

                // output log to console
                string responseStatus = $"\nRequest Status >> Reason >> {statusCode}\n";
                string stats =
                    $"Start Time: {_startTime}  |  On Air: {_mainTimer.Elapsed}  |  Execution Speed: {watcher.ElapsedMilliseconds / 1000.0}  - | Api Calls: {TotalApiCalls} |" +
                    $"  - OFFERS DATA >> Total: {TotalOffersCounter} -- Accepted: {TotalAcceptedOffers}";

                Console.WriteLine(responseStatus);
                Console.WriteLine(stats);

                Thread log = new Thread((() => Log(responseStatus, stats)));
                log.Start();

                if (statusCode is HttpStatusCode.Unauthorized || statusCode is HttpStatusCode.Forbidden)
                {
                    // Re-authenticate after the access token has expired
                    GetAccessToken();
                    Thread.Sleep(10000); // 10 seconds
                    continue;
                }

                if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
                {
                    // Request exceed. Send to SNS topic to terminate the instance. Put to sleep for 31 minutes
                    Thread.Sleep(ThrottlingTimeOut);
                    continue;
                }

                // restart counter to measure performance
                watcher.Restart();
            }
        }

        private void Log(string responseStatus, string stats)
        {
            var saveDict = new Dictionary<string, string>()
            {
                ["response"] = responseStatus,
                ["stats"] = stats,
            };

            _statsDict[UserId] = saveDict;
            StreamHandle.SaveJson(Path.Combine(RootPath, settings.Default.StatsPath, $"User-{UserId}.json"), _statsDict);
            StreamHandle.SaveStateFile(Path.Combine(RootPath, settings.Default.StateFile));
        }
    }
}