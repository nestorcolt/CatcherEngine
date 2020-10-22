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
        private const string ApiBaseUrl = "https://flex-capacity-na.amazon.com/";
        public static string AcceptUri { get; } = "AcceptOffer";
        public static string OffersUri { get; } = "GetOffersForProviderPost";
        public static string ServiceAreaUri { get; } = "eligibleServiceAreas";
        public const string TokenKeyConstant = "x-amz-access-token";
        public static HttpClient ApiClient { get; set; }


        public static void InitializeClient()
        {

            ApiClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

        public static void SetBaseAddress(string uri = null)
        {
            ApiClient.BaseAddress = uri is null ? new Uri(ApiBaseUrl) : new Uri(uri);
        }


        public static async Task<JToken> GetServiceAuthentication(string uri, string authToken)
        {

            if (ApiClient.BaseAddress.ToString() != ApiBaseUrl)
                SetBaseAddress();

            ApiClient.DefaultRequestHeaders.Add(TokenKeyConstant, authToken);
            var content = await GetDataAsync(uri);
            var value = content.GetValue("serviceAreaIds");
            return value;

        }

        public static async Task<JObject> GetDataAsync(string uri)
        {
            try
            {
                var content = await ApiClient.GetStringAsync(ServiceAreaUri);
                return await Task.Run(() => JObject.Parse(content));
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public static async Task<JObject> PostDataAsync(string uri)
        {
            try
            {
                var headers = ApiClient.DefaultRequestHeaders;
                string jsonData = JsonConvert.SerializeObject(headers);
                var response = await ApiClient.PostAsync(uri, new StringContent(jsonData));
                string result = response.Content.ReadAsStringAsync().Result;
                return await Task.Run(() => JObject.Parse(result));
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public static void AddRequestHeaders(Dictionary<string, object> headersDictionary)
        {
            ApiClient.DefaultRequestHeaders.Clear();

            foreach (var data in headersDictionary)
            {
                ApiClient.DefaultRequestHeaders.Add(data.Key, (string)data.Value);
            }
        }
    }
}
