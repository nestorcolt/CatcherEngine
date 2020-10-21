using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FlexCatcher
{
    public class ApiHelper

    {
        public static HttpClient ApiClient { get; set; }
        private static String ApiBaseURL = "https://flex-capacity-na.amazon.com/";

        public static void InitializeClient()
        {
            ApiClient = new HttpClient { BaseAddress = new Uri(ApiBaseURL) };
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

    }
}