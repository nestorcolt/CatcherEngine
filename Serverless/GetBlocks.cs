using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using SearchEngine.Modules;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace SearchEngine.Serverless
{
    class GetBlocks
    {
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent userData, ILambdaContext context)
        {

            //BlockCatcher catcher = new BlockCatcher(userData);
            Console.WriteLine(userData.Records);
            //HttpStatusCode responseCode = await catcher.GetOffersAsyncHandle(userData);
            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();

        }
    }
}
