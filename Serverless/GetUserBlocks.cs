using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using SearchEngine.Properties;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine.Serverless
{
    class GetUserBlocks
    {
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent userData, ILambdaContext context)
        {
            settings.Default.Version = "Running Version: 26-01-2021 16:40";
            settings.Default.UserId = "11";

            //BlockCatcher catcher = new BlockCatcher(userData);
            foreach (var record in userData.Records)
            {
                var snsRecord = record.Sns;
                Console.WriteLine($"[{record.EventSource} {snsRecord.Timestamp}] Message = {snsRecord.Message}");
            }
            //HttpStatusCode responseCode = await catcher.GetOffersAsyncHandle(userData);
            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();

        }
    }
}
