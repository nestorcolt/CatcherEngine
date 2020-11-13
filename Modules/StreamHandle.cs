using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catcher.Modules
{
    public static class StreamHandle
    {

        public static async Task<JObject> LoadJsonAsync(string filePath)
        {
            using StreamReader jFiler = new StreamReader(filePath);
            JObject jsonTemplate = JObject.Parse(await jFiler.ReadToEndAsync());
            return jsonTemplate;
        }

        public static void SaveJson(string filePath, Dictionary<string, string> data)
        {
            using StreamWriter jFiler = new StreamWriter(filePath);
            JsonSerializer serializer = new JsonSerializer();
            //serialize object directly into file stream
            serializer.Serialize(jFiler, data);
        }

    }
}