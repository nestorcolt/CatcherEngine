using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlexCatcher
{
    class BlockCatcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        string ownerEndpointURL = "https://www.thunderflex.us/admin/script_functions.php";
        private string _offersDirectory = "/GetOffersForProviderPost";
        private string _serviceAreaDirectory = "https://flex-capacity-na.amazon.com/eligibleServiceAreas";
        private static string _apiBaseURL = "https://flex-capacity-na.amazon.com";

        private readonly string _userId;
        private readonly string _flexAppVersion;
        private Dictionary<string, string> _offersDataHeader;
        private bool _accessSuccessCode;

        public BlockCatcher(string userId, string flexAppVersion)
        {
            ApiHelper.InitializeClient();
            _flexAppVersion = flexAppVersion;
            _userId = userId;

            // Primary methods resolution
            EmulateDevice();
            GetAccessData();
            SetServiceArea();

            // Main loop method is being called here
            if (_accessSuccessCode)
            {
                //LookingForBlocks();
                Console.WriteLine("Looking for blocks 1, 2, 3 ...");
            }



        }

        private int GetTimestamp()
        {

            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        private void SetServiceArea()
        {

            WebHeaderCollection data = new WebHeaderCollection();

            foreach (var innerData in _offersDataHeader)
            {
                data.Add(innerData.Key, innerData.Value);
            }

            string response = GetAsync(_serviceAreaDirectory, data).Result;
            var responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(response);
            var serviceAreaId = responseDictionary["serviceAreaIds"][0];

            // Id Dictionary to parse to offer headers later
            var serviceDataDictionary = new Dictionary<string, object>

            {
                ["serviceAreaIds"] = $"[{serviceAreaId}]",
                ["apiVersion"] = "V2",
                ["filters"] = new Dictionary<string, object>
                {
                    ["serviceAreaFilter"] = new List<string>(),
                    ["timeFilter"] = new Dictionary<string, string>(),
                },

            };

            string jsonData = JsonConvert.SerializeObject(serviceDataDictionary);
            // TODO MERGE THE HEADERS OFFERS AND SERVICE DATA IN ONE.
            // here ... 

        }

        public void GetAccessData()
        {

            var data = new Dictionary<string, string>

            {
                { "userId", _userId },
                { "action", "access_token" }

            };
            string jsonData = JsonConvert.SerializeObject(data);
            string response = PostAsync(ownerEndpointURL, jsonData).Result;
            Dictionary<string, string> responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

            if (responseDictionary["access_token"] == "failed")
            {
                Console.WriteLine("Session token request failed. Operation aborted.\n");
                _accessSuccessCode = false;
            }

            else
            {
                _offersDataHeader["x-amz-access-token"] = responseDictionary["access_token"];
                Console.WriteLine("Access to the service granted!\n");
                _accessSuccessCode = true;
            }


        }

        private void EmulateDevice()
        {
            var data = new Dictionary<string, string>

            {
                { "userId", _userId },
                { "action", "instance_id" }

            };

            String jsonData = JsonConvert.SerializeObject(data);
            String response = PostAsync(ownerEndpointURL, jsonData).Result;
            Dictionary<string, string> responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);


            string androidVersion = responseDictionary["androidVersion"];
            string deviceModel = responseDictionary["deviceModel"];
            string instanceId = responseDictionary["instanceId"];
            string build = responseDictionary["build"];
            string uuid = Guid.NewGuid().ToString();
            int time = GetTimestamp();

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["X-Flex-Client-Time"] = time.ToString(),
                ["Content-Type"] = "application/json",
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} {build}) RabbitAndroid/{_flexAppVersion}",
                ["X-Amzn-RequestId"] = uuid,
                ["Host"] = "flex-capacity-na.amazon.com",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            // Set the class field with the new offer headers
            _offersDataHeader = offerAcceptHeaders;

        }


        public async Task<string> GetAsync(string uri, WebHeaderCollection data)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Headers = data;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public async Task<string> PostAsync(string uri, string data, string method = "POST")
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = "application/json";
            request.Method = method;


            await using (Stream requestBody = request.GetRequestStream())
            {
                await requestBody.WriteAsync(dataBytes, 0, dataBytes.Length);
            }

            using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            await using Stream stream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private void AcceptOffer()
        {

        }

        private void ValidateOffers()
        {

        }

        private void GetOffers()
        {

            String jsonData = JsonConvert.SerializeObject(_offersDataHeader);
            String response = PostAsync(_offersDirectory, jsonData).Result;
            Dictionary<string, string> responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Console.WriteLine(response);
        }


        public void LookingForBlocks()
        {
            int counter = 0;
            while (true)

            {
                GetOffers();
                counter++;

                if (counter == 10)
                    break;
            }
        }

    }
}