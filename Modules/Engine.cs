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
        protected Dictionary<string, string> RequestDataHeadersDictionary = new Dictionary<string, string>();

        protected string ServiceAreaFilterData;
        protected UserDto Authenticator;

        public const string TokenKeyConstant = "x-amz-access-token";

        public JToken SearchSchedule;
        protected string RefreshToken;
        protected string AccessToken;
        protected string TimeZone;
        public List<string> Areas;
        public float MinimumPrice;
        public string UserId;

        public bool ProcessSucceed { get; set; }

        public string AppVersion => settings.Default.FlexAppVersion;

        public void InitializeEngine()
        {

            ProcessSucceed = false;

            UserId = Authenticator.UserId;
            AccessToken = Authenticator.AccessToken;
            RefreshToken = Authenticator.RefreshToken;
            MinimumPrice = Authenticator.MinimumPrice;
            SearchSchedule = Authenticator.SearchSchedule;
            TimeZone = Authenticator.TimeZone;
            Areas = Authenticator.Areas;

            // Set token in request dictionary
            RequestDataHeadersDictionary[TokenKeyConstant] = AccessToken;

            // HttpClients are init here
            ApiHelper.InitializeClient();

            // Primary methods resolution to get access to the request headers
            EmulateDevice();

            // Set the client service area to sent as extra data with the request on get blocks method
            SetServiceArea();

            // set headers to clients
            ApiHelper.AddRequestHeaders(RequestDataHeadersDictionary);
        }

        private string GetServiceAreaId()
        {
            ApiHelper.ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, AccessToken);
            HttpResponseMessage content = ApiHelper.GetDataAsync(ApiHelper.ServiceAreaUri).Result;

            if (content.IsSuccessStatusCode)
            {
                // continue if the validation succeed
                JObject requestToken = ApiHelper.GetRequestJTokenAsync(content).Result;
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
                RequestNewAccessToken();
                ProcessSucceed = false;
            }

            return null;
        }

        public void Log(string message)
        {
            CloudLogger.PublishToSnsAsync(message, String.Format(CloudLogger.UserLogStreamName, UserId)).Wait();
        }

        public void RequestNewAccessToken()
        {
            SendSnsMessage(AuthenticationSnsTopic, JsonConvert.SerializeObject(Authenticator)).Wait();
        }

        public async Task SendSnsMessage(string topicArn, string message)
        {
            IAmazonSimpleNotificationService client = new AmazonSimpleNotificationServiceClient();
            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = message
            };

            await client.PublishAsync(request);
        }

        public int GetTimestamp()
        {
            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private void SetServiceArea()
        {
            string serviceAreaId = GetServiceAreaId();

            if (String.IsNullOrEmpty(serviceAreaId))
                return;

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
            ServiceAreaFilterData = JsonConvert.SerializeObject(serviceDataDictionary).Replace("\\", "");
        }

        private void EmulateDevice()
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
                RequestDataHeadersDictionary[header.Key] = header.Value;
            }
        }
    }
}