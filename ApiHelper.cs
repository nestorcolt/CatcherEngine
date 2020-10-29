using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FlexCatcher.Properties;

namespace FlexCatcher
{
    public class ApiHelper
    {
        // main URLS
        private const string AcceptInputUrl = "http://internal.amazon.com/coral/com.amazon.omwbuseyservice.offers/";
        public static string OwnerEndpointUrl = "https://www.thunderflex.us/admin/script_functions.php";
        public const string ApiBaseUrl = "https://flex-capacity-na.amazon.com/";

        // directories
        public static string AcceptUri = "AcceptOffer";
        public static string OffersUri = "GetOffersForProviderPost";
        public static string AssignedBlocks = "scheduledAssignments";
        public static string ScheduleBlocks = "schedule/blocks";
        public static string ServiceAreaUri = "eligibleServiceAreas";
        public static string Areas = "pooledServiceAreasForProvider";
        public static string Regions = "regions";

        public static HttpClient ApiClient { get; set; }
        public static HttpClient CatcherClient { get; set; }
        public static HttpClientHandler CatcherClientHandler { get; set; }
        public static HttpClient SeekerClient { get; set; }
        public static HttpClientHandler SeekerClientHandler { get; set; }
        private static bool Debug = settings.Default.debug;

        public const string TokenKeyConstant = "x-amz-access-token";
        public static int TotalAcceptedOffers;

        public static void InitializeClient()
        {
            ApiClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            int maxConnectionsPerServerThreshold = 500;

            CatcherClientHandler = new HttpClientHandler();
            CatcherClientHandler.UseDefaultCredentials = true;
            CatcherClientHandler.MaxConnectionsPerServer = maxConnectionsPerServerThreshold;
            CatcherClient = new HttpClient(CatcherClientHandler) { BaseAddress = new Uri(ApiBaseUrl) };

            SeekerClientHandler = new HttpClientHandler();
            SeekerClientHandler.UseDefaultCredentials = true;
            SeekerClientHandler.MaxConnectionsPerServer = maxConnectionsPerServerThreshold;
            SeekerClient = new HttpClient(SeekerClientHandler) { BaseAddress = new Uri(ApiBaseUrl) };

            int connectionLimitThreshold = 10000;
            ServicePointManager.DefaultConnectionLimit = connectionLimitThreshold;
            SetMaxConcurrency(ApiBaseUrl, connectionLimitThreshold);

            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

        public static async Task<JToken> GetServiceAuthentication(string uri, string authToken)
        {

            ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, authToken);
            var content = await GetDataAsync(uri);
            JObject requestToken = await GetRequestTokenAsync(content);
            JToken value = requestToken.GetValue("serviceAreaIds");
            return value;

        }

        private static void SetMaxConcurrency(string url, int maxConcurrentRequests)
        {
            ServicePointManager.FindServicePoint(new Uri(url)).ConnectionLimit = maxConcurrentRequests;
        }

        public static async Task<HttpResponseMessage> GetBlockFromDataBaseAsync(string uri, string authToken)
        {

            ApiClient.DefaultRequestHeaders.Clear();
            ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, authToken);
            HttpResponseMessage content = await ApiClient.GetAsync(uri);
            return content;

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

        public static async Task<HttpResponseMessage> PostDataAsync(string uri, string data, HttpClient customClient = null)
        {
            HttpClient client = customClient ?? ApiClient;

            try
            {
                HttpResponseMessage response = await client.PostAsync(uri, new StringContent(data));
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public static async Task<JObject> GetRequestTokenAsync(HttpResponseMessage requestMessage)
        {
            string content = await requestMessage.Content.ReadAsStringAsync();
            return await Task.Run(() => JObject.Parse(content));
        }

        public static void AddRequestHeaders(Dictionary<string, string> headersDictionary, HttpClient customClient)
        {
            HttpClient client = customClient ?? ApiClient;
            client.DefaultRequestHeaders.Clear();

            foreach (var data in headersDictionary)
            {
                client.DefaultRequestHeaders.Add(data.Key, data.Value);
            }
        }

        public static async Task AcceptOfferAsync(string offerId)
        {
            var acceptHeader = new Dictionary<string, string>
            {
                {"__type", $"AcceptOfferInput:{AcceptInputUrl}"},
                {"offerId", offerId}
            };

            string jsonData = JsonConvert.SerializeObject(acceptHeader);
            HttpResponseMessage response = await PostDataAsync(ApiHelper.AcceptUri, jsonData, CatcherClient);

            if (response.IsSuccessStatusCode)
                // send to owner endpoint accept data to log and send to the user the notification
                TotalAcceptedOffers++;


            if (ApiHelper.Debug)
                Console.WriteLine($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n");
        }

        public static async Task DeleteOfferAsync(int blockId)
        {
            string url = ScheduleBlocks + "/" + blockId.ToString();
            await ApiClient.DeleteAsync(url);
        }

    }
}
