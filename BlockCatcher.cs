﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        readonly DateTime _startTime;

        private int _totalOffersCounter;
        private int _totalApiCalls;
        private int _totalRejectedOffers;
        private int _totalAcceptedOffers;
        private bool _cleanUpOffers;

        public bool AccessSuccess;
        private int _speed;

        public bool Debug { get; set; }

        private SignatureObject Signature { get; }
        private Stopwatch MainTimer { get; }
        public bool ApiIsThrottling { get; set; }
        public int AfterThrottlingTimeOut { get; set; }


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
                Console.WriteLine("Session token request failed. Operation aborted.\n");
                AccessSuccess = false;
            }

            else
            {
                _offersDataHeader[ApiHelper.TokenKeyConstant] = responseValue;
                Console.WriteLine("Access to the service granted!\n");
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

            Parallel.ForEach(blocksArray.Values(), async block =>
            {
                if (!block.HasValues)
                    return;

                JToken innerBlock = block[0];
                JToken serviceAreaId = innerBlock["serviceAreaId"];
                JToken offerPrice = innerBlock["bookedPrice"]["amount"];
                JToken startTime = innerBlock["startTime"];

                // The time the offer will be available for pick up at the facility
                int pickUpTimespan = (int)startTime - GetTimestamp();

                if ((float)offerPrice < _minimumPrice || !_areas.Contains((string)serviceAreaId) || pickUpTimespan < _pickUpTimeThreshold)
                {
                    await ApiHelper.DeleteOfferAsync((int)startTime);
                    _totalRejectedOffers++;
                }

            });

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

        private SortedDictionary<string, string> SignRequestHeaders(string url)
        {
            return Signature.CreateSignature(url, _offersDataHeader[ApiHelper.TokenKeyConstant]);

        }

        public async Task<List<string>> AcceptOffersAsync(HttpResponseMessage response)
        {

            var outputTuple = new List<string>();

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestTokenAsync(response);
                var offerList = requestToken.GetValue("offerList");


                if (offerList != null && !offerList.HasValues)
                    return outputTuple;

                Parallel.For(0, offerList.Count(), async n =>
                {
                    await AcceptSingleOfferAsync(offerList[n]["offerId"].ToString());
                });

                _totalOffersCounter += offerList.Count();
                _cleanUpOffers = true;
            }
            return outputTuple;
        }


        private async Task<HttpResponseMessage> FetchOffers()
        {

            SortedDictionary<string, string> signatureHeaders = SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");
            _offersDataHeader["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
            _offersDataHeader["X-Flex-Client-Time"] = GetTimestamp().ToString();
            _offersDataHeader["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
            _offersDataHeader["Authorization"] = signatureHeaders["Authorization"];
            ApiHelper.AddRequestHeaders(_offersDataHeader, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_offersDataHeader, ApiHelper.CatcherClient);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, _serviceAreaFilterData, ApiHelper.SeekerClient);

            return response;

        }

        public async Task LookingForBlocks()
        {
            Stopwatch watcher = Stopwatch.StartNew();

            while (true)

            {
                // start logic here
                HttpResponseMessage response = await FetchOffers();

                if (response.IsSuccessStatusCode)
                {
                    Thread acceptThread = new Thread(async task => await AcceptOffersAsync(response));
                    acceptThread.Start();
                    _totalApiCalls++;
                }

                else if (response.StatusCode is HttpStatusCode.Unauthorized || response.StatusCode is HttpStatusCode.Forbidden)
                {

                    GetAccessData().Wait();
                    Thread.Sleep(10000);
                    continue;
                }

                else if (response.StatusCode is HttpStatusCode.BadRequest || response.StatusCode is HttpStatusCode.TooManyRequests)
                {

                    Thread.Sleep(AfterThrottlingTimeOut);

                }

                //Will launch the catch offers clean up every time an offers is accepted
                if (_cleanUpOffers)
                {
                    Thread validateThread = new Thread(async task => await ValidateOffers());
                    validateThread.Start();
                    _cleanUpOffers = false;
                }

                // custom delay to save request
                Thread.Sleep(_speed);

                if (Debug)
                {
                    Console.WriteLine($"\nRequest Status >> Reason >> {response.StatusCode}\n");
                    // output log to console
                    Console.WriteLine($"Start Time: {_startTime}  |  On Air: {MainTimer.Elapsed}  |  Execution Speed: {watcher.ElapsedMilliseconds / 1000.0}  - | Api Calls: {_totalApiCalls} |" +
                                      $"  - OFFERS DATA >> Total: {_totalOffersCounter} -- Accepted: {_totalAcceptedOffers} -- Rejected: {_totalRejectedOffers} -- " +
                                      $"Lost: {_totalOffersCounter - _totalAcceptedOffers}");
                }

                // restart counter to measure performance
                watcher.Restart();

            }

        }
    }
}