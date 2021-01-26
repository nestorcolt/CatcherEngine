﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;

namespace SearchEngine.Modules
{
    class BlockCatcher : Engine
    {
        private readonly SignatureObject _signature = new SignatureObject();
        private readonly Stopwatch _mainTimer = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.Now;

        public BlockCatcher(UserDto authenticator)
        {
            // This can be changed to SNSEvents for serverless
            Authenticator = authenticator;

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
            //Console.WriteLine($"Schedule validated: {scheduleValidation}");

            bool areaValidation = ValidateArea(serviceAreaId);
            //Console.WriteLine($"Area validated: {areaValidation}");

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

        private void CreateStreams(string statusCode, long elapsed)
        {
            // output log to console
            string responseStatus = $"\nRequest Status >> Reason >> {statusCode}\n";
            string stats =
                $"{settings.Default.Version} | Start Time: {_startTime}  |  On Air: {_mainTimer.Elapsed}  |" +
                $" Execution Speed: {elapsed / 1000.0}  - | Api Calls: {TotalApiCalls} |" +
                $" - OFFERS DATA >> Total: {TotalOffersCounter} -- Accepted: {TotalAcceptedOffers}";

            // Logs to cloud watch
            if (CloudLogger.SecondsCounter >= CloudLogger.SendMessageInSecondsThreshold)
            {
                // restore counter
                CloudLogger.SecondsCounter = 0;

                // send the message
                Log(responseStatus);
                Log(stats);
            }

            CloudLogger.SecondsCounter++;
        }

        public async Task LookingForBlocksLegacy()
        {
            Stopwatch watcher = Stopwatch.StartNew();
            Log($"\t- User {UserId} looking for blocks...");

            // start logic here main request
            HttpStatusCode statusCode = await GetOffersAsyncHandle();

            // custom delay to save request
            Thread.Sleep((int)Speed);

            // Stream Logs
            CreateStreams(statusCode.ToString(), watcher.ElapsedMilliseconds);

            if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
            {
                // Request exceed. Send to SNS topic to terminate the instance. Put to sleep for 31 minutes
                Log($"\nRequest Status >> Reason >> {statusCode}\n");
                SendSnsMessage(SleepSnsTopic, UserId).Wait();
            }
        }
    }
}