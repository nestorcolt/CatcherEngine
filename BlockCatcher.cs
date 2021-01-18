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

namespace SearchEngine
{
    class BlockCatcher : Engine
    {
        private readonly SignatureObject _signature = new SignatureObject();
        private readonly Stopwatch _mainTimer = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.Now;

        private readonly string _rootPath;
        private Dictionary<string, object> _statsDict = new Dictionary<string, object>();

        public BlockCatcher()
        {
            // setup engine details
            InitializeEngine();

            //get the full location of the assembly with DaoTests in it
            _rootPath = AppDomain.CurrentDomain.BaseDirectory;

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

        public async Task AcceptSingleOfferAsync(JToken block)
        {

            DateTime timeNow = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)timeNow).ToUnixTimeSeconds();
            long offerTime = (long)block["startTime"];

            // get the time span in minutes which this block will need to be pick up
            long blockTimeSpan = (offerTime - unixTime) / 60; // 60 seconds per minute

            // check some data first if not pass this validation is not necessary to execute a new date time call or math operation in next validation 
            string serviceAreaId = (string)block["serviceAreaId"];
            float offerPrice = (float)block["rateInfo"]["priceAmount"];

            // TODO WILL REMOVE LATER, THIS WAS FOR DOUBLE CHECKING AND DEBUG THE VALIDATION DATA
            //Console.WriteLine(offerPrice >= MinimumPrice);
            //Console.WriteLine(blockTimeSpan >= ArrivalTimeSpan);
            //Console.WriteLine(Areas.Contains(serviceAreaId));
            //Console.WriteLine($"{offerPrice} {MinimumPrice}");
            //Console.WriteLine($"{blockTimeSpan} {ArrivalTimeSpan}");
            //Console.WriteLine($"{serviceAreaId}");

            // ArrivalTimeSpan comes in minutes from user filters
            if (blockTimeSpan >= ArrivalTimeSpan && offerPrice >= MinimumPrice && Areas.Contains(serviceAreaId))
            {
                string offerId = block["offerId"].ToString();
                var acceptHeader = new Dictionary<string, string>
                {
                    {"__type", $"AcceptOfferInput:{ ApiHelper.AcceptInputUrl}"},
                    {"offerId", offerId}
                };

                string jsonData = JsonConvert.SerializeObject(acceptHeader);
                HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.AcceptUri, jsonData, ApiHelper.CatcherClient);

                if (response.IsSuccessStatusCode)
                {
                    // send to owner endpoint accept data to log and send to the user the notification
                    TotalAcceptedOffers++;
                }

                if (Debug)
                    Console.WriteLine($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n");
            }

        }

        public void AcceptOffers(JToken offerList)
        {
            Parallel.For(0, offerList.Count(), n =>
            {
                JToken innerBlock = offerList[n];
                ////Thread accept = new Thread(async task => await AcceptSingleOfferAsync(innerBlock));
                //accept.Start();
            });
        }

        public void LookingForBlocksLegacy()
        {
            Stopwatch watcher = Stopwatch.StartNew();

            while (true)

            {
                // start logic here main request
                HttpStatusCode statusCode = GetOffersAsyncHandle().Result;

                // custom delay to save request
                Thread.Sleep((int)Speed);

                if (Debug)
                {
                    // output log to console
                    string responseStatus = $"\nRequest Status >> Reason >> {statusCode}\n";
                    string stats = $"Start Time: {_startTime}  |  On Air: {_mainTimer.Elapsed}  |  Execution Speed: {watcher.ElapsedMilliseconds / 1000.0}  - | Api Calls: {TotalApiCalls} |" +
                                      $"  - OFFERS DATA >> Total: {TotalOffersCounter} -- Accepted: {TotalAcceptedOffers}";

                    Console.WriteLine(responseStatus);
                    Console.WriteLine(stats);

                    Thread log = new Thread((() => Log(responseStatus, stats)));
                    log.Start();

                }

                if (statusCode is HttpStatusCode.Unauthorized || statusCode is HttpStatusCode.Forbidden)
                {
                    AccessToken = Authenticator.GetAmazonAccessToken(RefreshToken).Result;
                    Thread.Sleep(10000); // 10 seconds
                    continue;
                }

                if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
                {
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
            StreamHandle.SaveJson(Path.Combine(_rootPath, "stats.json"), _statsDict);
        }
    }
}