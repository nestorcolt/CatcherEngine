using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;



// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)


namespace FlexCatcher
{
    class Program
    {
        static void Main(string[] args)

        {
            try
            {
                var catcher = new BlockCatcher();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }

    class BlockCatcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {

        string serviceAreaURL = "https://flex-capacity-na.amazon.com/eligibleServiceAreas";
        string ownerEndpointURL = "https://www.thunderflex.us/admin/script_functions.php";
        string offersURL = "https://flex-capacity-na.amazon.com/GetOffersForProviderPost";
        string acceptURL = "https://flex-capacity-na.amazon.com/AcceptOffer";



        public BlockCatcher()
        {
            Login();
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

        public async Task<string> PostAsync(string uri, string data, string contentType, string method = "POST")
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = contentType;
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

        public void Login()
        {

            // TODO Will be implement the current user here
            var data = new Dictionary<String, String>

            {
                { "userId", "100" },
                { "action", "access_token" }

            };

            String jsonData = JsonConvert.SerializeObject(data);
            String response = PostAsync(ownerEndpointURL, jsonData, "application/json", "POST").Result;
            Dictionary<string, string> responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

            if (responseDictionary["access_token"] == "failed")
                Console.WriteLine("Session Token request failed.");
            else
                // the token will be save to a class property dictionary here
                Console.WriteLine(response);


        }

        private void EmulateDevice()
        {

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
