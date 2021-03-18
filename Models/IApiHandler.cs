using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchEngine.Models
{
    public interface IApiHandler
    {
        Task<HttpResponseMessage> GetDataAsync(string uri);
        Task<HttpResponseMessage> PostDataAsync(string uri, string data);
        Task<JObject> GetRequestJTokenAsync(HttpResponseMessage requestMessage);
        void AddRequestHeaders(Dictionary<string, string> headersDictionary);
    }
}