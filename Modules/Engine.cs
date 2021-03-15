using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    class Engine

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        protected string AuthenticationSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-AUTHENTICATE-TOPIC";
        protected string AcceptedSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-ACCEPTED-TOPIC";
        protected string OffersSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-OFFERS-TOPIC";
        protected string SleepSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-SLEEP-TOPIC";
        protected string StopSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-STOP-TOPIC";

        public const string TokenKeyConstant = "x-amz-access-token";
        public string AppVersion => settings.Default.FlexAppVersion;
        public bool ProcessSucceed { get; set; }
        public string UserPk = "user_id";

        public async Task RequestNewAccessToken(UserDto userDto)
        {
            await SnsHandler.PublishToSnsAsync(JsonConvert.SerializeObject(userDto), "", AuthenticationSnsTopic);
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
                    ProcessSucceed = true;
                    return (string)result[0];
                }
            }

            // validations on errors
            if (content.StatusCode is HttpStatusCode.Unauthorized || content.StatusCode is HttpStatusCode.Forbidden)
            {
                // Re-authenticate after the access token has expired
                await RequestNewAccessToken(userDto);
                ProcessSucceed = false;
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
    }
}