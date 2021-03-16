﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    class BlockCatcher
    {
        protected string AuthenticationSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-AUTHENTICATE-TOPIC";
        protected string AcceptedSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-ACCEPTED-TOPIC";
        protected string OffersSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-OFFERS-TOPIC";
        protected string SleepSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-SLEEP-TOPIC";
        protected string StopSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-STOP-TOPIC";

        //private readonly SignatureObject _signature = new SignatureObject();
        public const string TokenKeyConstant = "x-amz-access-token";
        public string AppVersion => settings.Default.FlexAppVersion;
        public string UserPk = "user_id";

        private async Task DeactivateUser(string userId)
        {
            await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(UserPk, userId)).ToString(), "msg", StopSnsTopic);
        }

        private bool ScheduleHasData(JToken searchSchedule)
        {
            if (searchSchedule != null && searchSchedule.HasValues)
                return true;

            return false;
        }

        private bool ValidateArea(string serviceAreaId, List<string> areas)
        {
            if (areas.Count == 0)
                return true;

            if (areas.Contains(serviceAreaId))
                return true;

            return false;
        }

        //private void SignRequestHeaders(string url)
        //{
        //    SortedDictionary<string, string> signatureHeaders = _signature.CreateSignature(url, AccessToken);

        //    RequestDataHeadersDictionary["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
        //    RequestDataHeadersDictionary["X-Flex-Client-Time"] = GetTimestamp().ToString();
        //    RequestDataHeadersDictionary["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
        //    RequestDataHeadersDictionary["Authorization"] = signatureHeaders["Authorization"];
        //}

        public async Task RequestNewAccessToken(UserDto userDto)
        {
            await SnsHandler.PublishToSnsAsync(JsonConvert.SerializeObject(userDto), "msg", AuthenticationSnsTopic);
        }

        public int GetTimestamp()
        {
            TimeSpan time = (DateTime.UtcNow - DateTime.UnixEpoch);
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        public async Task<string> GetServiceAreaId(UserDto userDto)
        {
            ApiHelper.ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, userDto.AccessToken);
            HttpResponseMessage content = await ApiHelper.GetDataAsync(ApiHelper.ServiceAreaUri);

            if (content.IsSuccessStatusCode)
            {
                // continue if the validation succeed
                JObject requestToken = await ApiHelper.GetRequestJTokenAsync(content);
                JToken result = requestToken.GetValue("serviceAreaIds");

                if (result.HasValues)
                {
                    return (string)result[0];
                }
            }

            // validations on errors
            if (content.StatusCode is HttpStatusCode.Unauthorized || content.StatusCode is HttpStatusCode.Forbidden)
            {
                // Re-authenticate after the access token has expired
                await RequestNewAccessToken(userDto);
            }

            return null;
        }

        public async Task<string> SetServiceArea(UserDto userDto)
        {
            string serviceAreaId = await GetServiceAreaId(userDto);

            if (String.IsNullOrEmpty(serviceAreaId))
                return null;

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
            string serviceAreaFilterData = JsonConvert.SerializeObject(serviceDataDictionary).Replace("\\", "");
            return serviceAreaFilterData;
        }

        public Dictionary<string, string> EmulateDevice(Dictionary<string, string> requestDictionary)
        {
            string instanceId = Guid.NewGuid().ToString().Replace("-", "");
            string androidVersion = settings.Default.OSVersion;
            string deviceModel = settings.Default.DeviceModel;
            string build = settings.Default.BuildVersion;

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} Build/{build}) RabbitAndroid/{AppVersion}",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            // Set the class field with the new offer headers
            foreach (var header in offerAcceptHeaders)
            {
                requestDictionary[header.Key] = header.Value;
            }

            return requestDictionary;
        }
        public async Task AcceptSingleOfferAsync(JToken block, UserDto userDto)
        {
            bool isValidated = false;
            long offerTime = (long)block["startTime"];
            string serviceAreaId = (string)block["serviceAreaId"];
            float offerPrice = (float)block["rateInfo"]["priceAmount"];

            // Validates the calendar schedule for this user
            bool scheduleValidation = false;
            bool areaValidation = false;

            Parallel.Invoke(() => scheduleValidation = ScheduleValidator.ValidateSchedule(userDto.SearchSchedule, offerTime, userDto.TimeZone),
                () => areaValidation = ValidateArea(serviceAreaId, userDto.Areas));

            if (scheduleValidation && offerPrice >= userDto.MinimumPrice && areaValidation)
            {
                // to track in offers table
                isValidated = true;
                string offerId = block["offerId"].ToString();

                JObject acceptHeader = new JObject(
                    new JProperty("__type", $"AcceptOfferInput:{ApiHelper.AcceptInputUrl}"),
                    new JProperty("offerId", offerId)
                );

                HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.AcceptUri, acceptHeader.ToString());

                if (response.IsSuccessStatusCode)
                {
                    // send to owner endpoint accept data to log and send to the user the notification
                    JObject data = new JObject(
                        new JProperty(UserPk, userDto.UserId),
                        new JProperty("data", block)
                        );

                    // LOGS FOR ACCEPTED OFFERS
                    await SnsHandler.PublishToSnsAsync(data.ToString(), "msg", AcceptedSnsTopic);
                }

                // test to log in cloud watch
                await CloudLogger.Log($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n", userDto.UserId);
            }

            // send the offer seen to the offers table for further data processing or analytic
            JObject offerSeen = new JObject(
                new JProperty(UserPk, userDto.UserId),
                new JProperty("validated", isValidated),
                new JProperty("data", block)
            );

            // LOGS FOR SEEN OFFERS
            await SnsHandler.PublishToSnsAsync(offerSeen.ToString(), "msg", OffersSnsTopic);
        }

        public void AcceptOffers(JToken offerList, UserDto userDto)
        {
            Parallel.For(0, offerList.Count(), n =>
            {
                JToken innerBlock = offerList[n];
                Thread accept = new Thread(async task => await AcceptSingleOfferAsync(innerBlock, userDto));
                accept.Start();
            });
        }

        private async Task<HttpStatusCode> GetOffersAsyncHandle(UserDto userDto, Dictionary<string, string> requestHeaders, string serviceAreaId)
        {
            //SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");
            ApiHelper.AddRequestHeaders(requestHeaders);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, serviceAreaId);

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && offerList.HasValues)
                {
                    Thread acceptThread = new Thread(task => AcceptOffers(offerList, userDto));
                    acceptThread.Start();
                }
            }

            return response.StatusCode;
        }

        public async Task<bool> LookingForBlocks(UserDto userDto)
        {
            // validator of weekly schedule
            if (!ScheduleHasData(userDto.SearchSchedule))
            {
                await DeactivateUser(userDto.UserId);
                return false;
            }

            // HttpClients are init here
            ApiHelper.InitializeClient();

            // Start filling the headers
            var requestHeaders = new Dictionary<string, string>();

            // Set token in request dictionary
            requestHeaders[TokenKeyConstant] = userDto.AccessToken;

            // Primary methods resolution to get access to the request headers
            requestHeaders = EmulateDevice(requestHeaders);

            // Set the client service area to sent as extra data with the request on get blocks method
            string serviceAreaId = await SetServiceArea(userDto);

            // validation before continue
            if (String.IsNullOrEmpty(serviceAreaId))
                return false;

            // start logic here main request
            HttpStatusCode statusCode = await GetOffersAsyncHandle(userDto, requestHeaders, serviceAreaId);

            if (statusCode is HttpStatusCode.OK)
            {
                return true;
            }

            if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
            {
                // Request exceed. Send to SNS topic to terminate the instance. Put to sleep for 30 minutes
                await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(UserPk, userDto.UserId)).ToString(), "msg", SleepSnsTopic);

                // Stream Logs
                string responseStatus = $"\nRequest Status >> Reason >> {statusCode} | The system will pause for 30 minutes\n";
                await CloudLogger.Log(responseStatus, userDto.UserId);
            }

            return false;
        }
    }
}