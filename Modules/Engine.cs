using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Catcher.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CatcherEngine.Modules
{
    class Engine

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        protected Dictionary<string, string> _requestDataHeadersDictionary = new Dictionary<string, string>();
        protected string ServiceAreaFilterData;
        protected int TotalOffersCounter;
        protected int TotalAcceptedOffers;
        protected int TotalApiCalls;

        protected int ThrottlingTimeOut = settings.Default.ExecutionTimeOut * 60000;
        protected int Speed = (int)((settings.Default.ExecutionSpeed - settings.Default.SpeedOffset) * 1000.0f);


        public const string TokenKeyConstant = "x-amz-access-token";
        protected string CurrentUserToken;
        public string UserId;

        public bool Debug => settings.Default.Debug;
        public string AppVersion => settings.Default.FlexAppVersion;

        public void InitializeEngine(string userId)
        {
            UserId = userId;
            Console.WriteLine("Catcher: Initializing Engine...");

            // HttpClients are init here
            ApiHelper.InitializeClient();

            // Primary methods resolution to get access to the request headers
            Task.Run(GetAccessDataAsync).Wait();
            EmulateDevice();

            // Set the client service area to sent as extra data with the request on get blocks method
            SetServiceArea();

            // set headers to clients
            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.CatcherClient);

        }

        protected async Task<HttpResponseMessage> GetBlockFromDataBaseAsync(string uri)
        {

            ApiHelper.ApiClient.DefaultRequestHeaders.Clear();
            ApiHelper.ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, CurrentUserToken);
            HttpResponseMessage content = await ApiHelper.ApiClient.GetAsync(uri);
            return content;

        }

        public int GetTimestamp()
        {

            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private string GetServiceAreaId()
        {

            ApiHelper.ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, CurrentUserToken);
            var content = ApiHelper.GetDataAsync(ApiHelper.ServiceAreaUri).Result;
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

        public async Task<bool> GetAccessDataAsync()
        {
            var data = new Dictionary<string, object>
            {
                { "userId", UserId },
                { "action", "access_token" }
            };

            string jsonData = JsonConvert.SerializeObject(data);
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
            string responseValue = requestToken.GetValue("access_token").ToString();

            if (responseValue == "failed")
            {
                Console.WriteLine("\nSession token request failed. Operation aborted.\n");
                return false;
            }

            _requestDataHeadersDictionary[TokenKeyConstant] = responseValue;
            CurrentUserToken = responseValue;
            Console.WriteLine("\nAccess to the service granted!\n");
            return true;
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
                _requestDataHeadersDictionary[header.Key] = header.Value;
            }
        }
    }
}