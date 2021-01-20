using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;

namespace SearchEngine.Modules
{
    class Engine

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        protected Dictionary<string, string> RequestDataHeadersDictionary = new Dictionary<string, string>();
        protected string ServiceAreaFilterData;
        protected int TotalOffersCounter;
        protected int TotalAcceptedOffers;
        protected int TotalApiCalls;

        protected int ThrottlingTimeOut = settings.Default.ExecutionTimeOut * 60000;
        protected readonly Authenticator Authenticator = new Authenticator();

        public const string TokenKeyConstant = "x-amz-access-token";
        public List<JToken> SearchSchedule;
        protected string RefreshToken;
        protected string AccessToken;
        public List<string> Areas;
        public float MinimumPrice;
        protected float Speed;
        public string UserId;

        public string AppVersion => settings.Default.FlexAppVersion;

        public void InitializeEngine()
        {
            // Get user data from dynamo DB through and Ec2 instance private IP matching with user ID
            Authenticator.Authenticate();

            UserId = Authenticator.UserId;
            AccessToken = Authenticator.AccessToken;
            RefreshToken = Authenticator.RefreshToken;
            MinimumPrice = Authenticator.MinimumPrice;
            SearchSchedule = Authenticator.SearchSchedule;
            Areas = Authenticator.Areas;

            // Set token in request dictionary
            RequestDataHeadersDictionary[TokenKeyConstant] = AccessToken;

            // set bot speed delay
            SetSpeed(Authenticator.Speed);

            // HttpClients are init here
            ApiHelper.InitializeClient();

            // Primary methods resolution to get access to the request headers
            EmulateDevice();

            // Set the client service area to sent as extra data with the request on get blocks method
            SetServiceArea();

            // set headers to clients
            ApiHelper.AddRequestHeaders(RequestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(RequestDataHeadersDictionary, ApiHelper.CatcherClient);

            Console.WriteLine($"Catcher: Initializing Engine on user {UserId} ...");

        }


        public void GetAccessToken()
        {
            AccessToken = Authenticator.GetAmazonAccessToken(RefreshToken).Result;
            RequestDataHeadersDictionary[TokenKeyConstant] = AccessToken;
            Console.WriteLine("\nAccess to the service granted!\n");
        }


        public void SetSpeed(float speed)
        {
            Speed = (int)((speed - settings.Default.SpeedOffset) * 1000.0f);
        }

        public int GetTimestamp()
        {

            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private string GetServiceAreaId()
        {

            ApiHelper.ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, AccessToken);
            HttpResponseMessage content = ApiHelper.GetDataAsync(ApiHelper.ServiceAreaUri).Result;
            JObject requestToken = ApiHelper.GetRequestJTokenAsync(content).Result;
            JToken result = requestToken.GetValue("serviceAreaIds");

            if (result.HasValues)
                return (string)result[0];

            return null;
        }

        private void SetServiceArea()
        {
            string serviceAreaId = GetServiceAreaId();

            if (serviceAreaId.Length == 0)
                throw new InvalidOperationException();

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