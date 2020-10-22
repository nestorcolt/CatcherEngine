using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexCatcher
{
    class BlockCatcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        private Dictionary<string, string> _offersDataHeader;
        private readonly string _flexAppVersion;
        private readonly string _userId;
        private string _serviceAreaFilterData;
        private bool _accessSuccessCode;

        public BlockCatcher(string userId, string flexAppVersion)
        {
            ApiHelper.InitializeClient();
            _flexAppVersion = flexAppVersion;
            _userId = userId;

            // Primary methods resolution
            Task.Run(EmulateDevice).Wait();
            Task.Run(GetAccessData).Wait();
            SetServiceArea();
            // Main loop method is being called here
            if (_accessSuccessCode)
            {
                LookingForBlocks();
                Console.WriteLine("Looking for blocks 1, 2, 3 ...");
            }

        }

        private int GetTimestamp()
        {

            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }


        private string GetServiceAreaId()
        {
            var result = ApiHelper.GetServiceAuthentication(ApiHelper.ServiceAreaUri, _offersDataHeader[ApiHelper.TokenKeyConstant]).Result;

            if (result.HasValues)
                return (string)result[0];

            return null;
        }

        private void SetServiceArea()
        {

            var serviceAreaId = GetServiceAreaId();
            var filtersDict = new Dictionary<string, object>
            {
                ["serviceAreaFilter"] = new List<string>(),
                ["timeFilter"] = new Dictionary<string, string>(),
            };

            var stringFilters = JsonConvert.SerializeObject(filtersDict);

            // Id Dictionary to parse to offer headers later
            var serviceDataDictionary = new Dictionary<string, object>

            {
                ["serviceAreaIds"] = $"[{serviceAreaId}]",
                ["apiVersion"] = "V2",
                ["filters"] = stringFilters,

            };

            // MERGE THE HEADERS OFFERS AND SERVICE DATA IN ONE MAIN HEADER DICTIONARY
            _serviceAreaFilterData = JsonConvert.SerializeObject(serviceDataDictionary);

        }

        public async Task GetAccessData()
        {
            var data = new Dictionary<string, object>

            {
                { "userId", _userId },
                { "action", "access_token" }

            };
            string jsonData = JsonConvert.SerializeObject(data);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            string responseValue = response.GetValue("access_token").ToString();

            if (responseValue == "failed")
            {
                Console.WriteLine("Session token request failed. Operation aborted.\n");
                _accessSuccessCode = false;
            }

            else
            {
                _offersDataHeader[ApiHelper.TokenKeyConstant] = responseValue;
                Console.WriteLine("Access to the service granted!\n");
                _accessSuccessCode = true;
            }

        }

        private async Task EmulateDevice()
        {
            var data = new Dictionary<string, string>
            {
                { "userId", _userId },
                { "action", "instance_id" }

            };

            String jsonData = JsonConvert.SerializeObject(data);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);

            string androidVersion = response.GetValue("androidVersion").ToString();
            string deviceModel = response.GetValue("deviceModel").ToString();
            string instanceId = response.GetValue("instanceId").ToString();
            string build = response.GetValue("build").ToString();
            string uuid = Guid.NewGuid().ToString();
            int time = GetTimestamp();

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["X-Flex-Client-Time"] = time.ToString(),
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} {build}) RabbitAndroid/{_flexAppVersion}",
                ["X-Amzn-RequestId"] = uuid,
                ["Host"] = "flex-capacity-na.amazon.com",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            // Set the class field with the new offer headers
            _offersDataHeader = offerAcceptHeaders;
        }


        private void AcceptOffer()
        {

        }

        private void ValidateOffers()
        {

        }

        private async Task GetOffers()
        {
            foreach (var mine in _offersDataHeader)
            {
                Console.WriteLine(mine.Key);
            }


            ApiHelper.AddRequestHeaders(_offersDataHeader);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, _serviceAreaFilterData);
        }


        public void LookingForBlocks()
        {
            int counter = 0;
            while (true)

            {
                Task.Run(GetOffers).Wait();
                counter++;

                if (counter == 1)
                    break;
            }
        }

    }
}