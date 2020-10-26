using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FlexCatcher
{
    public class ApiHelper
    {
        private const string AcceptInputUrl = "http://internal.amazon.com/coral/com.amazon.omwbuseyservice.offers/";
        public static string OwnerEndpointUrl = "https://www.thunderflex.us/admin/script_functions.php";

        private const string ApiBaseUrl = "https://flex-capacity-na.amazon.com/";
        public static string AcceptUri = "AcceptOffer";
        public static string OffersUri = "GetOffersForProviderPost";
        public static string AssignedBlocks = "scheduledAssignments";
        public static string ScheduleBlocks = "schedule/blocks";
        public static string ServiceAreaUri = "eligibleServiceAreas";
        public static string Areas = "pooledServiceAreasForProvider";
        public static string Regions = "regions";

        public static HttpClient ApiClient { get; set; }
        //public static HttpClient DisposableClient { get; set; }
        public static HttpClient CatcherClient { get; set; }
        public static HttpClient SeekerClient { get; set; }
        public const string TokenKeyConstant = "x-amz-access-token";

        public static void InitializeClient()
        {
            //DisposableClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            CatcherClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            SeekerClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            ApiClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };

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

        public static async Task<JToken> GetPool(string uri, string authToken)
        {

            ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, authToken);
            HttpResponseMessage content = await GetDataAsync(uri);
            JObject requestToken = await GetRequestTokenAsync(content);
            return requestToken;
        }

        public static async Task<JObject> GetBlockFromDataBaseAsync(string uri, string authToken)
        {

            ApiClient.DefaultRequestHeaders.Clear();
            ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, authToken);
            HttpResponseMessage content = await ApiClient.GetAsync(uri);
            JObject requestToken = await GetRequestTokenAsync(content);
            return requestToken;

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

        public static void AddRequestHeaders(Dictionary<string, string> headersDictionary, HttpClient customClient = null)
        {
            HttpClient client = customClient ?? ApiClient;
            client.DefaultRequestHeaders.Clear();

            foreach (var data in headersDictionary)
            {
                client.DefaultRequestHeaders.Add(data.Key, (string)data.Value);
            }
        }

        public static async Task<HttpResponseMessage> AcceptOfferAsync(string offerId)
        {
            var acceptHeader = new Dictionary<string, string>
            {
                {"__type", $"AcceptOfferInput:{AcceptInputUrl}"},
                {"offerId", offerId}
            };

            string jsonData = JsonConvert.SerializeObject(acceptHeader);
            HttpResponseMessage response = await PostDataAsync(ApiHelper.AcceptUri, jsonData, CatcherClient);
            return response;
        }

        public static async Task DeleteOfferAsync(int blockId)
        {
            string url = ScheduleBlocks + "/" + blockId.ToString();
            await ApiClient.DeleteAsync(url);
        }

    }
}
