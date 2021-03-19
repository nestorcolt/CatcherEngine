using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using SearchEngine.Lib;
using System.Threading.Tasks;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine.Serverless
{
    class CleanUpBlocksTable
    {

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            await DynamoHandler.DeleteBlocksTable();
            return "OK";
        }
    }
}