using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    public class ApiHandler
    {
        public static HttpClient ServiceAreaClient = new HttpClient() { BaseAddress = new Uri(Constants.ApiBaseUrl) };
        public static HttpClientHandler ClientHandler { get; set; }
        public static HttpClient ApiClient { get; set; }

        public ApiHandler()
        {
            InitializeClient();
        }

        public void InitializeClient()
        {
            int maxConnectionsPerServerThreshold = 500;

            ClientHandler = new HttpClientHandler
            {
                UseDefaultCredentials = true,
                MaxConnectionsPerServer = maxConnectionsPerServerThreshold
            };

            ApiClient = new HttpClient(ClientHandler) { BaseAddress = new Uri(Constants.ApiBaseUrl) };

            int connectionLimitThreshold = 10000;
            ServicePointManager.DefaultConnectionLimit = connectionLimitThreshold;
            SetMaxConcurrency(Constants.ApiBaseUrl, connectionLimitThreshold);

            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static void SetMaxConcurrency(string url, int maxConcurrentRequests)
        {
            ServicePointManager.FindServicePoint(new Uri(url)).ConnectionLimit = maxConcurrentRequests;
        }

        public static async Task<HttpResponseMessage> GetDataAsync(string uri, HttpClient customClient = null)
        {
            HttpClient client = customClient ?? ApiClient;

            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public static async Task<HttpResponseMessage> PostDataAsync(string uri, string data)
        {
            try
            {
                HttpResponseMessage response = await ApiClient.PostAsync(uri, new StringContent(data));
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public static async Task<JObject> GetRequestJTokenAsync(HttpResponseMessage requestMessage)
        {
            string content = await requestMessage.Content.ReadAsStringAsync();
            return await Task.Run(() => JObject.Parse(content));
        }

        public static void AddRequestHeaders(Dictionary<string, string> headersDictionary)
        {
            ApiClient.DefaultRequestHeaders.Clear();

            foreach (var data in headersDictionary)
            {
                ApiClient.DefaultRequestHeaders.Add(data.Key, data.Value);
            }
        }
    }
}
