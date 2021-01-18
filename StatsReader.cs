using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;

namespace SearchEngine
{
    class StatsReader
    {
        public StatsReader()
        {
            Console.WriteLine("--------------------------------------------------------------------------------------------------------------------");

            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(rootPath, settings.Default.StatsPath);

            using StreamReader jFiler = new StreamReader(path);
            JObject jsonTemplate = JObject.Parse(jFiler.ReadToEnd());


            foreach (var data in jsonTemplate)
            {
                Console.WriteLine(data.Key.ToUpper());
                var values = data.Value.ToObject<Dictionary<string, object>>();

                foreach (var innerDict in values)
                {
                    Console.WriteLine(innerDict.Value);
                }

                Console.WriteLine("--------------------------------------------------------------------------------------------------------------------");
            }
        }
    }
}
