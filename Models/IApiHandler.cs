using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    public interface IApiHandler
    {
        Task<HttpResponseMessage> GetDataAsync(string uri);
        Task<HttpResponseMessage> PostDataAsync(string uri, string data);
        Task<JObject> GetRequestJTokenAsync(HttpResponseMessage requestMessage);
        void AddRequestHeaders(Dictionary<string, string> headersDictionary);
    }
}