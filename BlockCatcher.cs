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
using FlexCatcher.Properties;

namespace FlexCatcher
{
    class BlockCatcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {

        private Dictionary<string, string> _offersDataHeader;
        private string _serviceAreaFilterData;
        private readonly string _flexAppVersion;
        private readonly float _minimumPrice;
        private readonly int _pickUpTimeThreshold;
        private readonly string[] _areas;
        private readonly string _userId;
        private HttpResponseMessage _currentOfferRequestObject;
        readonly DateTime _startTime;

        private int _totalOffersCounter;
        private int _totalApiCalls;
        private int _totalRejectedOffers;
        private int _totalAcceptedOffers;

        public bool AccessSuccess;
        private int _throttlingTimeOut;
        private int _speed;
        private int _cleanUpThresholdTime;

        public bool Debug { get; set; }
        public bool CleanUpAll { get; set; }
        private SignatureObject Signature { get; }
        private Stopwatch MainTimer { get; }


        public float AfterThrottlingTimeOut

        {
            get => _throttlingTimeOut;
            set => _throttlingTimeOut = (int)(value * 60000);
        }

        public float ExecutionSpeed
        {
            get => _speed;
            set => _speed = (int)(value * 1000);
        }

        public BlockCatcher(string userId, string flexAppVersion, float minimumPrice, int pickUpTimeThreshold, string[] areas)
        {
            Signature = new SignatureObject();
            MainTimer = Stopwatch.StartNew();

            _startTime = DateTime.Now;
            Debug = settings.Default.debug;

            _pickUpTimeThreshold = pickUpTimeThreshold;
            _flexAppVersion = flexAppVersion;
            _cleanUpThresholdTime = 120000;
            _minimumPrice = minimumPrice;
            _userId = userId;
            _areas = areas;

            ApiHelper.InitializeClient();

            // Primary methods resolution
            Task.Run(EmulateDevice).Wait();
            Task.Run(GetAccessData).Wait();

            // Set the client service area to sent as extra data with the request on get blocks method
            SetServiceArea();

            ApiHelper.AddRequestHeaders(_offersDataHeader, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_offersDataHeader, ApiHelper.CatcherClient);

        }

        private int GetTimestamp()
        {

            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private string GetServiceAreaId()
        {
            var result = ApiHelper.GetServiceAuthentication(ApiHelper.ServiceAreaUri, _offersDataHeader[ApiHelper.TokenKeyConstant]).Result;

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

        public async Task GetAccessData()
        {
            var data = new Dictionary<string, object>
            {
                { "userId", _userId },
                { "action", "access_token" }

            };

            string jsonData = JsonConvert.SerializeObject(data);
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            JObject requestToken = await ApiHelper.GetRequestTokenAsync(response);
            string responseValue = requestToken.GetValue("access_token").ToString();

            if (responseValue == "failed")
            {
                Console.WriteLine("\nSession token request failed. Operation aborted.\n");
                AccessSuccess = false;
            }

            else
            {
                _offersDataHeader[ApiHelper.TokenKeyConstant] = responseValue;
                Console.WriteLine("\nAccess to the service granted!\n");
                AccessSuccess = true;
            }

        }

        private async Task EmulateDevice()
        {
            var data = new Dictionary<string, string>
            {
                { "userId", _userId },
                { "action", "instance_id" }
            };

            String jsonData = JsonConvert.SerializeObject(data);
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            JObject requestToken = await ApiHelper.GetRequestTokenAsync(response);

            string androidVersion = requestToken.GetValue("androidVersion").ToString();
            string deviceModel = requestToken.GetValue("deviceModel").ToString();
            string instanceId = requestToken.GetValue("instanceId").ToString();
            string build = requestToken.GetValue("build").ToString();

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} {build}) RabbitAndroid/{_flexAppVersion}",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            // Set the class field with the new offer headers
            _offersDataHeader = offerAcceptHeaders;
        }

        private async Task ValidateOffers()

        {

            // if the validation is not success will try to find in the catch blocks the one did not passed the validation and forfeit them
            var response = await ApiHelper.GetBlockFromDataBaseAsync(ApiHelper.AssignedBlocks, _offersDataHeader[ApiHelper.TokenKeyConstant]);
            JObject blocksArray = await ApiHelper.GetRequestTokenAsync(response);

            if (!blocksArray.HasValues)
                return;

            foreach (var block in blocksArray.Values())
            {
                if (!block.HasValues)
                    continue;

                JToken innerBlock = block[0];
                JToken serviceAreaId = innerBlock["serviceAreaId"];
                JToken offerPrice = innerBlock["bookedPrice"]["amount"];
                JToken startTime = innerBlock["startTime"];

                // The time the offer will be available for pick up at the facility
                int pickUpTimespan = (int)startTime - GetTimestamp();


                if (CleanUpAll)
                {
                    await ApiHelper.DeleteOfferAsync((int)startTime);
                    _totalRejectedOffers++;
                }

                else if ((float)offerPrice < _minimumPrice || !_areas.Contains((string)serviceAreaId) || pickUpTimespan < _pickUpTimeThreshold)
                {
                    await ApiHelper.DeleteOfferAsync((int)startTime);
                    _totalRejectedOffers++;
                }

            }

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

        private void SignRequestHeaders(string url)
        {
            SortedDictionary<string, string> signatureHeaders = Signature.CreateSignature(url, _offersDataHeader[ApiHelper.TokenKeyConstant]);

            _offersDataHeader["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
            _offersDataHeader["X-Flex-Client-Time"] = GetTimestamp().ToString();
            _offersDataHeader["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
            _offersDataHeader["Authorization"] = signatureHeaders["Authorization"];
        }

        public async Task AcceptOffersAsync(HttpResponseMessage response)
        {

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && !offerList.HasValues)
                    return;

                Parallel.For(0, offerList.Count(), n =>
               {
                   Thread accept = new Thread(async task => await AcceptSingleOfferAsync(offerList[n]["offerId"].ToString()));
                   accept.Start();
               });

                _totalOffersCounter += offerList.Count();
            }

        }

        private async Task FetchOffers()
        {

            SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");
            ApiHelper.AddRequestHeaders(_offersDataHeader, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_offersDataHeader, ApiHelper.CatcherClient);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, _serviceAreaFilterData, ApiHelper.SeekerClient);
            _currentOfferRequestObject = response;

            if (response.IsSuccessStatusCode)
            {
                Thread acceptThread = new Thread(async task => await AcceptOffersAsync(response));
                acceptThread.Start();
                _totalApiCalls++;
            }

        }

        public void LookingForBlocks()
        {
            Stopwatch watcher = Stopwatch.StartNew();
            Stopwatch cleanWatcher = Stopwatch.StartNew();

            while (true)

            {
                // start logic here
                Thread requestThread = new Thread(async task => await FetchOffers());
                requestThread.Start();

                // as the first request process runs super fast because the multi-threading I validate if the _currentOfferRequestObject is null which means
                // the request hasn't been resolved yet. once this is done I can proceed with the logic.
                if (_currentOfferRequestObject is null)
                {
                    Thread.Sleep(_speed);
                    continue;
                }

                if (_currentOfferRequestObject.StatusCode is HttpStatusCode.Unauthorized || _currentOfferRequestObject.StatusCode is HttpStatusCode.Forbidden)
                {

                    GetAccessData().Wait();
                    Thread.Sleep(100000);
                    _currentOfferRequestObject.StatusCode = HttpStatusCode.OK;
                    continue;
                }

                if (_currentOfferRequestObject.StatusCode is HttpStatusCode.BadRequest || _currentOfferRequestObject.StatusCode is HttpStatusCode.TooManyRequests)
                {
                    // Set a variable to the Documents path.
                    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    // Append text to an existing file named "WriteLines.txt".
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "MaxRequestDebug.txt"), true))
                    {
                        outputFile.WriteLine($"BAD-REQUEST >> Start Time: {DateTime.Now}  |  On Air: {MainTimer.Elapsed}  |  Api Calls: {_totalApiCalls} |");
                    }

                    Thread.Sleep(_throttlingTimeOut);

                }

                //Will launch the catch offers clean up every time an offers is accepted
                if (cleanWatcher.ElapsedMilliseconds >= _cleanUpThresholdTime)
                {
                    Task.Run(ValidateOffers);
                    cleanWatcher.Restart();
                }

                if (Debug)
                {

                    Console.WriteLine($"\nRequest Status >> Reason >> {_currentOfferRequestObject.StatusCode}\n");
                    // output log to console
                    Console.WriteLine($"Start Time: {_startTime}  |  On Air: {MainTimer.Elapsed}  |  Execution Speed: {watcher.ElapsedMilliseconds / 1000.0}  - | Api Calls: {_totalApiCalls} |" +
                                      $"  - OFFERS DATA >> Total: {_totalOffersCounter} -- Accepted: {_totalAcceptedOffers} -- Rejected: {_totalRejectedOffers} -- " +
                                      $"Lost: {_totalOffersCounter - _totalAcceptedOffers}");
                }

                // custom delay to save request
                Thread.Sleep(_speed);

                // restart counter to measure performance
                watcher.Restart();

            }

        }
    }
}