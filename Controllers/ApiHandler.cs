using Newtonsoft.Json.Linq;
using SearchEngine.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchEngine.Controllers
{
    public class ApiHandler : IApiHandler
    {
        private readonly HttpClient _httpClient;

        public ApiHandler(HttpClient client)
        {
            _httpClient = client;
        }

        public async Task<HttpResponseMessage> GetDataAsync(string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public async Task<HttpResponseMessage> PostDataAsync(string uri, string data)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(uri, new StringContent(data));
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public async Task<JObject> GetRequestJTokenAsync(HttpResponseMessage requestMessage)
        {
            string content = await requestMessage.Content.ReadAsStringAsync();
            return await Task.Run(() => JObject.Parse(content));
        }

        public void AddRequestHeaders(Dictionary<string, string> headersDictionary)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            foreach (var data in headersDictionary)
            {
                _httpClient.DefaultRequestHeaders.Add(data.Key, data.Value);
            }
        }
    }
}