using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CatcherEngine.Modules;
using CatcherEngine.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CatcherEngine
{
    class BlockCatcher : Engine
    {
        private readonly SignatureObject _signature = new SignatureObject();
        private readonly Stopwatch _mainTimer = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.Now;

        // for testing on EC2
        public List<string> Areas;
        public float MinimumPrice;
        public int PickUpTimeThreshold;

        private readonly string _rootPath;
        private Dictionary<string, object> _statsDict = new Dictionary<string, object>();

        public BlockCatcher(string user)
        {
            InitializeEngine(userId: user);
            PickUpTimeThreshold = settings.Default.PickUpTime;
            MinimumPrice = settings.Default.MinimumPrice;

            //get the full location of the assembly with DaoTests in it
            _rootPath = AppDomain.CurrentDomain.BaseDirectory;

            Areas = new List<string>
            {
                "f9530032-4659-4a14-b9e1-19496d97f633",
                "d98c442b-9688-4427-97b9-59a4313c2f66",
                "29571892-da88-4089-83f0-24135852c2e4",
                "49d080a7-a765-47cf-a29e-0f1842958d4a",
                "fd440da5-dc81-43bf-9afe-f4910bfd4090",
                "b90b085e-874f-48da-8150-b0c215efff08",
                "5540b055-ee3c-4274-9997-de65191d6932",
                "a446e8f9-28fb-4744-ad3f-0098543227ab",
                "033311b9-a6dd-4cfb-b0b7-1b5ee998638b",
                "5eb3af65-0e4e-48d3-99ce-7eff7923c3da",
                "61153cd4-58b5-43bc-83db-bdecf569dcda",
                "8ffc6623-5837-42c0-beea-6ac50ef43faa",
                "7e6dd803-a8a3-4b64-9996-903f88cc5fe7",
                "1496f58f-ca2d-43c7-817b-ec2c3613390d",
                "8cf0c633-504b-4f56-91b1-de1c45ecccb0",
            };

        }

        private async Task<HttpStatusCode> GetOffersAsyncHandle()
        {
            SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");

            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.CatcherClient);

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
            SortedDictionary<string, string> signatureHeaders = _signature.CreateSignature(url, CurrentUserToken);

            _requestDataHeadersDictionary["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
            _requestDataHeadersDictionary["X-Flex-Client-Time"] = GetTimestamp().ToString();
            _requestDataHeadersDictionary["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
            _requestDataHeadersDictionary["Authorization"] = signatureHeaders["Authorization"];
        }

        public async Task AcceptSingleOfferAsync(JToken block)
        {

            if (!block.HasValues)
                return;

            float offerPrice = float.Parse(block["rateInfo"]["priceAmount"].ToString());
            int offerTime = int.Parse(block["startTime"].ToString());
            string serviceAreaId = block["serviceAreaId"].ToString();
            string offerId = block["offerId"].ToString();

            if (MinimumPrice > offerPrice && PickUpTimeThreshold > offerTime && Areas.Contains(serviceAreaId))
            {
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

                Console.WriteLine(block);
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

            while (true)

            {
                // start logic here main request
                HttpStatusCode statusCode = GetOffersAsyncHandle().Result;

                // custom delay to save request
                Thread.Sleep(Speed);

                if (Debug)
                {
                    // output log to console
                    string responseStatus = $"\nRequest Status >> Reason >> {statusCode}\n";
                    string stats = $"Start Time: {_startTime}  |  On Air: {_mainTimer.Elapsed}  |  Execution Speed: {watcher.ElapsedMilliseconds / 1000.0}  - | Api Calls: {TotalApiCalls} |" +
                                      $"  - OFFERS DATA >> Total: {TotalOffersCounter} -- Accepted: {TotalAcceptedOffers} -- Rejected: {settings.Default.RejectedBlocks}";

                    Console.WriteLine(responseStatus);
                    Console.WriteLine(stats);

                    Thread log = new Thread((() => Log(responseStatus, stats)));
                    log.Start();

                }

                if (statusCode is HttpStatusCode.Unauthorized || statusCode is HttpStatusCode.Forbidden)
                {
                    GetAccessDataAsync().Wait();
                    Thread.Sleep(100000);
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