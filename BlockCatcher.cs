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

        string serviceAreaURL = "https://flex-capacity-na.amazon.com/eligibleServiceAreas";
        string ownerEndpointURL = "https://www.thunderflex.us/admin/script_functions.php";
        string offersURL = "https://flex-capacity-na.amazon.com/GetOffersForProviderPost";
        string acceptURL = "https://flex-capacity-na.amazon.com/AcceptOffer";

        private String _userID { get; set; }


        public BlockCatcher(String UserID)
        {
            ApiHelper.InitializeClient();
            _userID = UserID;

            Login();
            EmulateDevice();
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


            using (Stream requestBody = request.GetRequestStream())
            {
                await requestBody.WriteAsync(dataBytes, 0, dataBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }


        public async Task<HttpResponseMessage> apiPostData(string url, string data, string method = "POST")
        {

            using (HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(url))
                return response;

        }

        public void Login()
        {

            var data = new Dictionary<String, String>

            {
                { "userId", _userID },
                { "action", "access_token" }

            };

            String jsonData = JsonConvert.SerializeObject(data);
            String response = PostAsync(ownerEndpointURL, jsonData).Result;
            Dictionary<string, string> responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

            if (responseDictionary["access_token"] == "failed")
                Console.WriteLine("Session Token request failed.");
            else
                // the token will be save to a class property dictionary here
                Console.WriteLine("Login Success!");


        }

        private void EmulateDevice()
        {
            var data = new Dictionary<String, String>

            {
                { "userId", _userID },
                { "action", "instance_id" }

            };

            String jsonData = JsonConvert.SerializeObject(data);
            String response = PostAsync(ownerEndpointURL, jsonData).Result;
            Dictionary<string, string> responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            //Console.WriteLine(response);
        }

        private void AcceptOffer()
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