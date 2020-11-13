﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Catcher.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catcher
{
    class BlockCatcher : Engine
    {
        private readonly SignatureObject _signature = new SignatureObject();
        private readonly Stopwatch _mainTimer = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.Now;

        // for testing on EC2
        private readonly BlockValidator _validator;
        private readonly List<string> _acceptedOffersIds = new List<string>();

        private string _rootPath;
        private Dictionary<string, object> _statsDict = new Dictionary<string, object>();

        public BlockCatcher(string user)
        {
            InitializeEngine(userId: user);
            _validator = new BlockValidator(user);

            //get the full location of the assembly with DaoTests in it
            _rootPath = AppDomain.CurrentDomain.BaseDirectory;

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
                    // TODO NOT ACCEPTING BLOCKS
                    //acceptThread.Start();

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

        public async Task AcceptSingleOfferAsync(string offerId, string offerTime)
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
                _acceptedOffersIds.Append(offerTime);
                await _validator.ValidateOffersAsyncHandle(_acceptedOffersIds);

            }

            if (Debug)
                Console.WriteLine($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n");
        }

        public void AcceptOffers(JToken offerList)
        {
            Parallel.For(0, offerList.Count(), n =>
            {
                JToken innerBlock = offerList[n];
                JToken startTime = innerBlock["startTime"];
                Thread accept = new Thread(async task => await AcceptSingleOfferAsync(offerList[n]["offerId"].ToString(), startTime.ToString()));
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
                                      $"  - OFFERS DATA >> Total: {TotalOffersCounter} -- Accepted: {TotalAcceptedOffers} -- Lost: {TotalOffersCounter - TotalAcceptedOffers}";

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