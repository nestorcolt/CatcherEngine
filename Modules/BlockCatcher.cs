using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    class BlockCatcher : Engine
    {
        //private readonly SignatureObject _signature = new SignatureObject();
        protected ScheduleValidator ScheduleValidator;

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

        public async Task AcceptSingleOfferAsync(JToken block, UserDto userDto)
        {
            bool isValidated = false;
            long offerTime = (long)block["startTime"];
            string serviceAreaId = (string)block["serviceAreaId"];
            float offerPrice = (float)block["rateInfo"]["priceAmount"];

            // Validates the calendar schedule for this user
            bool scheduleValidation = false;
            bool areaValidation = false;

            Parallel.Invoke(() => scheduleValidation = ScheduleValidator.ValidateSchedule(offerTime),
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
            if (ScheduleHasData(userDto.SearchSchedule))
            {
                // todo mover esto a dentro de la funcion
                ScheduleValidator = new ScheduleValidator(userDto.SearchSchedule, userDto.TimeZone);
            }
            else
            {
                await DeactivateUser(userDto.UserId);
                return false;
            }

            // Start filling the headers
            var requestHeaders = new Dictionary<string, string>();

            // Set token in request dictionary
            requestHeaders[TokenKeyConstant] = userDto.AccessToken;

            // HttpClients are init here
            ApiHelper.InitializeClient();

            // Primary methods resolution to get access to the request headers
            requestHeaders = EmulateDevice(requestHeaders);

            // Set the client service area to sent as extra data with the request on get blocks method
            string serviceAreaId = await SetServiceArea(userDto);

            // validation before continue
            if (String.IsNullOrEmpty(serviceAreaId))
                return false;

            // start logic here main request
            HttpStatusCode statusCode = await GetOffersAsyncHandle(userDto, requestHeaders, serviceAreaId);
            Console.WriteLine(statusCode.ToString());

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
                return false;
            }

            return false;
        }
    }
}