using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CatcherEngine.Modules;
using Newtonsoft.Json.Linq;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace CatcherEngine
{
    class CatchFunction
    {
        public BlockCatcher Catcher = new BlockCatcher();

        [Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> CatchHandle(JObject userId, ILambdaContext context)
        {

            string user = userId.ToString();
            HttpStatusCode responseCode = await Catcher.GetOffersAsyncHandle("4918");

            return $"Response code {responseCode}";

        }
    }
}
