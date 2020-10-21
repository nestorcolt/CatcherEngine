using System.Net.Http;

namespace FlexCatcher
{
    public class ApiHelper
    {
        public static HttpClient ApiClient { get; set; }
        private static string _apiBaseURL = "https://flex-capacity-na.amazon.com/";

        public static string OffersUri { get; } = "GetOffersForProviderPost";
        public static string AcceptUri { get; } = "AcceptOffer";
        public static string ServiceAreaUri { get; } = "eligibleServiceAreas";


        public static void InitializeClient()
        {
            //ApiClient = new HttpClient { BaseAddress = new Uri(_apiBaseURL) };
            ApiClient = new HttpClient();
            //ApiClient.DefaultRequestHeaders.Accept.Clear();
            //ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

    }
}