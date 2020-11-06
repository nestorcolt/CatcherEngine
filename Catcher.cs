using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FlexCatcher
{
    class Catcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        private Dictionary<string, string> _requestDataHeadersDictionary = new Dictionary<string, string>();
        private readonly SignatureObject _signature = new SignatureObject();
        private readonly Stopwatch _mainTimer = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.Now;
        private string _serviceAreaFilterData;
        private string _userId;

        private int _totalOffersCounter;
        private int _totalAcceptedOffers;
        private int _throttlingTimeOut;
        private int _totalApiCalls;
        private int _speed;

        public bool Debug { get; set; }
        public string AppVersion { get; set; }

        public float AfterThrottlingTimeOut

        {
            get => _throttlingTimeOut;
            set => _throttlingTimeOut = (int)(value * 60000);
        }

        public float ExecutionSpeed
        {
            get => _speed;
            set => _speed = (int)((value - 0.2f) * 1000);
        }

        public void InitializeObject(string userId)
        {
            _userId = userId;

            // HttpClients are init here
            ApiHelper.InitializeClient();

            // Primary methods resolution to get access to the request headers
            Task.Run(GetAccessDataAsync).Wait();
            Task.Run(EmulateDeviceAsync).Wait();

            // Set the client service area to sent as extra data with the request on get blocks method
            SetServiceArea();

            // set headers to clients
            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.CatcherClient);

        }

        public int GetTimestamp()
        {

            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private string GetServiceAreaId()
        {
            var result = ApiHelper.GetServiceAuthentication(ApiHelper.ServiceAreaUri, _requestDataHeadersDictionary[ApiHelper.TokenKeyConstant]).Result;

            if (result.HasValues)
                return (string)result[0];

            return null;
        }

        private void SetServiceArea()
        {
            string serviceAreaId = GetServiceAreaId();
            var filtersDict = new Dictionary<string, object>
            {
                ["serviceAreaFilter"] = new List<string>(),
                ["timeFilter"] = new Dictionary<string, string>(),
            };

            // Id Dictionary to parse to offer headers later
            var serviceDataDictionary = new Dictionary<string, object>
            {
                ["serviceAreaIds"] = new[] { serviceAreaId },
                ["filters"] = filtersDict,

            };

            // MERGE THE HEADERS OFFERS AND SERVICE DATA IN ONE MAIN HEADER DICTIONARY
            _serviceAreaFilterData = JsonConvert.SerializeObject(serviceDataDictionary).Replace("\\", "");
        }

        public async Task GetAccessDataAsync()
        {
            var data = new Dictionary<string, object>
            {
                { "userId", _userId },
                { "action", "access_token" }
            };

            string jsonData = JsonConvert.SerializeObject(data);
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
            string responseValue = requestToken.GetValue("access_token").ToString();

            if (responseValue == "failed")
            {
                Console.WriteLine("\nSession token request failed. Operation aborted.\n");
            }

            else
            {
                _requestDataHeadersDictionary[ApiHelper.TokenKeyConstant] = responseValue;
                Console.WriteLine("\nAccess to the service granted!\n");
            }
        }

        private async Task EmulateDeviceAsync()
        {
            var data = new Dictionary<string, string>
            {
                { "userId", _userId },
                { "action", "instance_id" }
            };

            string jsonData = JsonConvert.SerializeObject(data);
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);

            string androidVersion = requestToken.GetValue("androidVersion").ToString();
            string deviceModel = requestToken.GetValue("deviceModel").ToString();
            string instanceId = requestToken.GetValue("instanceId").ToString();
            string build = requestToken.GetValue("build").ToString();

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} {build}) RabbitAndroid/{AppVersion}",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            // Set the class field with the new offer headers
            foreach (var header in offerAcceptHeaders)
            {
                _requestDataHeadersDictionary[header.Key] = header.Value;
            }
        }

        private void SignRequestHeaders(string url)
        {
            SortedDictionary<string, string> signatureHeaders = _signature.CreateSignature(url, _requestDataHeadersDictionary[ApiHelper.TokenKeyConstant]);

            _requestDataHeadersDictionary["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
            _requestDataHeadersDictionary["X-Flex-Client-Time"] = GetTimestamp().ToString();
            _requestDataHeadersDictionary["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
            _requestDataHeadersDictionary["Authorization"] = signatureHeaders["Authorization"];
        }

        public async Task AcceptSingleOfferAsync(string offerId)
        {
            var acceptHeader = new Dictionary<string, string>
            {
                {"__type", $"AcceptOfferInput:{ ApiHelper.AcceptInputUrl}"},
                {"offerId", offerId}
            };

            string jsonData = JsonConvert.SerializeObject(acceptHeader);
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.AcceptUri, jsonData, ApiHelper.CatcherClient);

            if (response.IsSuccessStatusCode)
                // send to owner endpoint accept data to log and send to the user the notification
                _totalAcceptedOffers++;

            if (Debug)
                Console.WriteLine($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n");
        }

        public void AcceptOffers(JToken offerList)
        {
            Parallel.For(0, offerList.Count(), n =>
           {
               Thread accept = new Thread(async task => await AcceptSingleOfferAsync(offerList[n]["offerId"].ToString()));
               accept.Start();
           });
        }

        private async Task<HttpStatusCode> GetOffersAsyncHandle()
        {
            SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");

            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.CatcherClient);

            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, _serviceAreaFilterData, ApiHelper.SeekerClient);
            _totalApiCalls++;

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && offerList.HasValues)
                {
                    Thread acceptThread = new Thread(task => AcceptOffers(offerList));
                    acceptThread.Start();

                    _totalOffersCounter += offerList.Count();
                }
            }

            return response.StatusCode;
        }

        public void LookingForBlocksLegacy()
        {
            Stopwatch watcher = Stopwatch.StartNew();

            while (true)

            {
                // start logic here
                HttpStatusCode statusCode = GetOffersAsyncHandle().Result;

                // as the first request process runs super fast because the multi-threading I validate if the _currentOfferRequestObject is null which means
                // the request hasn't been resolved yet. once this is done I can proceed with the logic.

                if (statusCode is HttpStatusCode.Unauthorized || statusCode is HttpStatusCode.Forbidden)
                {

                    GetAccessDataAsync().Wait();
                    Thread.Sleep(100000);
                    continue;
                }

                if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
                {
                    // Set a variable to the Documents path.
                    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    // Append text to an existing file named "WriteLines.txt".
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "MaxRequestDebug.txt"), true))
                    {
                        outputFile.WriteLine($"BAD-REQUEST >> Start Time: {DateTime.Now}  |  On Air: {_mainTimer.Elapsed}  |  Api Calls: {_totalApiCalls} |");
                    }

                    Thread.Sleep(_throttlingTimeOut);
                }

                // custom delay to save request
                Thread.Sleep(_speed);

                if (Debug)
                {

                    Console.WriteLine($"\nRequest Status >> Reason >> {statusCode}\n");
                    // output log to console
                    Console.WriteLine($"Start Time: {_startTime}  |  On Air: {_mainTimer.Elapsed}  |  Execution Speed: {watcher.ElapsedMilliseconds / 1000.0}  - | Api Calls: {_totalApiCalls} |" +
                                      $"  - OFFERS DATA >> Total: {_totalOffersCounter} -- Accepted: {_totalAcceptedOffers} -- Lost: {_totalOffersCounter - _totalAcceptedOffers}");
                }

                // restart counter to measure performance
                watcher.Restart();

            }

        }
    }
}