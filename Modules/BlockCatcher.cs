﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    class BlockCatcher : Engine
    {
        private readonly SignatureObject _signature = new SignatureObject();
        protected ScheduleValidator ScheduleValidator;

        public BlockCatcher(UserDto authenticator)
        {
            // The DTO object for the current user filters
            Authenticator = authenticator;

            // setup engine details
            InitializeEngine();

            // validator of weekly schedule
            ScheduleValidator = new ScheduleValidator(SearchSchedule);
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
            bool isValidated = false;
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
                // to track in offers table
                isValidated = true;

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
                    JObject data = new JObject(
                        new JProperty("user_id", UserId),
                        new JProperty("data", block)
                        );

                    await SendSnsMessage(AcceptedSnsTopic, data.ToString());
                }

                Log($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n");
            }

            // send the offer seen to the offers table for further data processing or analytic
            JObject offerSeen = new JObject(
                new JProperty("user_id", UserId),
                new JProperty("validated", isValidated),
                new JProperty("data", block)
            );

            await SendSnsMessage(OffersSnsTopic, offerSeen.ToString());
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
        private async Task<HttpStatusCode> GetOffersAsyncHandle()
        {
            SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");

            ApiHelper.AddRequestHeaders(RequestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(RequestDataHeadersDictionary, ApiHelper.CatcherClient);

            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, ServiceAreaFilterData, ApiHelper.SeekerClient);

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && offerList.HasValues)
                {
                    Thread acceptThread = new Thread(task => AcceptOffers(offerList));
                    acceptThread.Start();

                    // Stream Logs
                    string responseStatus = $"\nBlocks Found In Response >> {offerList.Count()} blocks\n";
                    Log(responseStatus);
                }
            }

            return response.StatusCode;
        }

        public async Task LookingForBlocks()
        {
            // start logic here main request
            HttpStatusCode statusCode = await GetOffersAsyncHandle();

            if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
            {
                // Request exceed. Send to SNS topic to terminate the instance. Put to sleep for 31 minutes
                SendSnsMessage(SleepSnsTopic, UserId).Wait();

                // Stream Logs
                string responseStatus = $"\nRequest Status >> Reason >> {statusCode} | The system will pause for 30 minutes\n";
                Log(responseStatus);
            }
        }
    }
}