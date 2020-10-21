using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FlexCatcher
{
    class BlockCatcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        string ownerEndpointURL = "https://www.thunderflex.us/admin/script_functions.php";
        private readonly string _userId;
        private readonly string _flexAppVersion;
        private readonly Dictionary<string, string> DeviceDataHeader;
        private bool _AccessSuccessCode = false;



        public BlockCatcher(string userId, string flexAppVersion)
        {
            ApiHelper.InitializeClient();
            _userId = userId;
            _flexAppVersion = flexAppVersion;

            // Methods resolution
            DeviceDataHeader = EmulateDevice();
            GetAccessData();
            Console.WriteLine(DeviceDataHeader["x-amz-access-token"]);
        }

        public async Task<string> GetAsync(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

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


        public async Task<HttpResponseMessage> apiPostData(string uri, string data, string method = "POST")
        {

            using HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(uri);
            return response;

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
                Console.WriteLine("Session token request failed.");
                _AccessSuccessCode = false;
            }

            else
            {
                DeviceDataHeader["x-amz-access-token"] = responseDictionary["access_token"];
                Console.WriteLine("Login Success!");
                _AccessSuccessCode = true;
            }


        }

        private Dictionary<string, string> EmulateDevice()
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
            DateTime now = DateTime.Now;


            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["X-Flex-Client-Time"] = now.TimeOfDay.ToString(),
                ["Content-Type"] = "application/json",
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} {build}) RabbitAndroid/{_flexAppVersion}",
                ["X-Amzn-RequestId"] = uuid,
                ["Host"] = "flex-capacity-na.amazon.com",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            return offerAcceptHeaders;
        }



        private void AcceptOffer()
        {

        }

        private void ValidateOffers()
        {

        }


        private void GetOffers()
        {

        }


        public void LookingForBlocks()
        {

        }

    }
}