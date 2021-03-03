using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine.Serverless
{
    class GetUserBlocks
    {
        private readonly BlockCatcher _catcher = new BlockCatcher();

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            JObject oldData = JObject.Parse(snsEvent.Records[0].Sns.Message);
            string newData = DynamoHandler.QueryUser(oldData["user_id"].ToString());
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(newData);
            bool recursive = false;

            try
            {
                _catcher.BlockCatcherInit(userDto);
                recursive = _catcher.LookingForBlocks();
            }
            catch (Exception e)
            {
                await CloudLogger.PublishToSnsAsync(message: e.ToString(), subject: $"User-{userDto.UserId}");
            }
            finally
            {
                // call trigger SNS to call again this lambda (recursion) base on recursion parameter
                if (recursive && userDto.SearchBlocks)
                {
                    // pass userData SNSEvent
                    await CloudLogger.PublishToSnsAsync(newData, "recursive", snsEvent.Records[0].Sns.TopicArn);
                }
            }

            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();
        }
    }
}