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
    static class BlockCatcher
    {
        //public readonly SignatureObject _signature = new SignatureObject();

        private static async Task DeactivateUser(string userId)
        {
            await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(Constants.UserPk, userId)).ToString(), "msg", Constants.StopSnsTopic);
        }

        private static bool ScheduleHasData(JToken searchSchedule)
        {
            if (searchSchedule != null && searchSchedule.HasValues)
                return true;

            return false;
        }

        private static bool ValidateArea(string serviceAreaId, List<string> areas)
        {
            if (areas.Count == 0)
                return true;

            if (areas.Contains(serviceAreaId))
                return true;

            return false;
        }

        //public void SignRequestHeaders(string url)
        //{
        //    SortedDictionary<string, string> signatureHeaders = _signature.CreateSignature(url, AccessToken);

        //    RequestDataHeadersDictionary["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
        //    RequestDataHeadersDictionary["X-Flex-Client-Time"] = GetTimestamp().ToString();
        //    RequestDataHeadersDictionary["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
        //    RequestDataHeadersDictionary["Authorization"] = signatureHeaders["Authorization"];
        //}

        public static int GetTimestamp()
        {
            TimeSpan time = (DateTime.UtcNow - DateTime.UnixEpoch);
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private static Dictionary<string, string> EmulateDevice(Dictionary<string, string> requestDictionary)
        {
            string instanceId = Guid.NewGuid().ToString().Replace("-", "");
            string androidVersion = settings.Default.OSVersion;
            string deviceModel = settings.Default.DeviceModel;
            string build = settings.Default.BuildVersion;

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} Build/{build}) RabbitAndroid/{Constants.AppVersion}",
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

        private static async Task AcceptSingleOfferAsync(JToken block, UserDto userDto)
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
                    new JProperty("__type", $"AcceptOfferInput:{Constants.AcceptInputUrl}"),
                    new JProperty("offerId", offerId)
                );

                HttpResponseMessage response = await ApiHandler.PostDataAsync(Constants.AcceptUri, acceptHeader.ToString());

                if (response.IsSuccessStatusCode)
                {
                    // send to owner endpoint accept data to log and send to the user the notification
                    JObject data = new JObject(
                        new JProperty(Constants.UserPk, userDto.UserId),
                        new JProperty("data", block)
                        );

                    // LOGS FOR ACCEPTED OFFERS
                    await SnsHandler.PublishToSnsAsync(data.ToString(), "msg", Constants.AcceptedSnsTopic);
                }

                // test to log in cloud watch
                await CloudLogger.Log($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n", userDto.UserId);
            }

            // send the offer seen to the offers table for further data processing or analytic
            JObject offerSeen = new JObject(
                new JProperty(Constants.UserPk, userDto.UserId),
                new JProperty("validated", isValidated),
                new JProperty("data", block)
            );

            // LOGS FOR SEEN OFFERS
            await SnsHandler.PublishToSnsAsync(offerSeen.ToString(), "msg", Constants.OffersSnsTopic);
        }

        private static void AcceptOffers(JToken offerList, UserDto userDto)
        {
            Parallel.For(0, offerList.Count(), n =>
            {
                JToken innerBlock = offerList[n];
                Thread accept = new Thread(async task => await AcceptSingleOfferAsync(innerBlock, userDto));
                accept.Start();
            });
        }

        private static async Task<HttpStatusCode> GetOffersAsyncHandle(UserDto userDto, Dictionary<string, string> requestHeaders, string serviceAreaId)
        {
            //SignRequestHeaders($"{ApiHandler.ApiBaseUrl}{ApiHandler.OffersUri}");
            ApiHandler.AddRequestHeaders(requestHeaders);
            var response = await ApiHandler.PostDataAsync(Constants.OffersUri, serviceAreaId);

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHandler.GetRequestJTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && offerList.HasValues)
                {
                    Thread acceptThread = new Thread(task => AcceptOffers(offerList, userDto));
                    acceptThread.Start();
                }
            }

            return response.StatusCode;
        }

        public static async Task<bool> LookingForBlocks(UserDto userDto)
        {
            ApiHandler.InitializeClient();

            // validator of weekly schedule
            if (!ScheduleHasData(userDto.SearchSchedule))
            {
                await DeactivateUser(userDto.UserId);
                return false;
            }

            // Start filling the headers
            var requestHeaders = new Dictionary<string, string>();

            // Set token in request dictionary
            requestHeaders[Constants.TokenKeyConstant] = userDto.AccessToken;

            // Primary methods resolution to get access to the request headers
            requestHeaders = EmulateDevice(requestHeaders);

            // validation before continue
            if (String.IsNullOrEmpty(userDto.ServiceAreaHeader))
            {
                await CloudLogger.Log("Service area ID was empty or null. Re-trying authentication...", userDto.UserId);
                await Authenticator.RequestNewAccessToken(userDto);
                return false;
            }

            // start logic here main request
            HttpStatusCode statusCode = await GetOffersAsyncHandle(userDto, requestHeaders, userDto.ServiceAreaHeader);

            if (statusCode is HttpStatusCode.OK)
            {
                return true;
            }

            if (statusCode is HttpStatusCode.Unauthorized || statusCode is HttpStatusCode.Forbidden)
            {
                // Re-authenticate after the access token has expired
                await Authenticator.RequestNewAccessToken(userDto);
            }

            else if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
            {
                // Request exceed. Send to SNS topic to terminate the instance. Put to sleep for 30 minutes
                await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(Constants.UserPk, userDto.UserId)).ToString(), "msg", Constants.SleepSnsTopic);

                // Stream Logs
                string responseStatus = $"\nRequest Status >> Reason >> {statusCode} | The system will pause for 30 minutes\n";
                await CloudLogger.Log(responseStatus, userDto.UserId);
            }

            return false;
        }
    }
}