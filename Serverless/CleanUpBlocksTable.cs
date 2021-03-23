using Amazon.Lambda.Core;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using CloudLibrary.Lib;

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