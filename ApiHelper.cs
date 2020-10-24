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
        public static string OwnerEndpointUrl = "https://www.thunderflex.us/admin/script_functions.php";
        private const string ApiBaseUrl = "https://flex-capacity-na.amazon.com/";
        private const string AcceptInputUrl = "http://internal.amazon.com/coral/com.amazon.omwbuseyservice.offers/";
        public static string AcceptUri { get; } = "AcceptOffer";
        public static string OffersUri { get; } = "GetOffersForProviderPost";
        public static string ServiceAreaUri { get; } = "eligibleServiceAreas";
        public static HttpClient ApiClient { get; set; }
        public static HttpResponseMessage CurrentResponse { get; set; }

        public const string TokenKeyConstant = "x-amz-access-token";

        public static void InitializeClient()
        {

            ApiClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

        public static async Task<JToken> GetServiceAuthentication(string uri, string authToken)
        {

            ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, authToken);
            var content = await GetDataAsync(uri);
            var value = content.GetValue("serviceAreaIds");
            return value;

        }

        public static async Task<JObject> GetDataAsync(string uri)
        {
            try
            {
                CurrentResponse = await ApiClient.GetAsync(uri);
                var content = await CurrentResponse.Content.ReadAsStringAsync();
                return await Task.Run(() => JObject.Parse(content));
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public static async Task<JObject> PostDataAsync(string uri, string data)
        {
            try
            {
                CurrentResponse = await ApiClient.PostAsync(uri, new StringContent(data));
                CurrentResponse.EnsureSuccessStatusCode();
                string content = await CurrentResponse.Content.ReadAsStringAsync();
                return await Task.Run(() => JObject.Parse(content));
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public static void AddRequestHeaders(Dictionary<string, string> headersDictionary)
        {
            ApiClient.DefaultRequestHeaders.Clear();

            foreach (var data in headersDictionary)
            {
                ApiClient.DefaultRequestHeaders.Add(data.Key, (string)data.Value);
            }
        }

        public static async Task AcceptOfferAsync(string offerId, Dictionary<string, string> offerHeaders)
        {
            var acceptHeader = new Dictionary<string, string>
            {
                {"__type", $"AcceptOfferInput:{AcceptInputUrl}"},
                {"offerId", offerId}
            };

            string jsonData = JsonConvert.SerializeObject(acceptHeader);
            ApiHelper.AddRequestHeaders(offerHeaders);
            var response = await ApiHelper.PostDataAsync(ApiHelper.AcceptUri, jsonData);
        }
    }
}
