using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
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
        public async Task<string> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            JObject oldData = JObject.Parse(sqsEvent.Records[0].Body);
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
                await CloudLogger.PublishToSnsAsync(e.ToString(), String.Format(CloudLogger.UserLogStreamName, userDto.UserId));
            }
            finally
            {
                // call trigger SQS to call again this lambda (recursion) base on recursion parameter
                if (recursive && userDto.SearchBlocks)
                {
                    // pass userData SQSEvent
                    string qUrl = await SqsHandler.GetQueueByName(SqsHandler.Client, SqsHandler.StartSearchQueueName);
                    await SqsHandler.SendMessage(SqsHandler.Client, qUrl, sqsEvent.Records[0].Body);
                }
            }

            // update last iteration value
            await DynamoHandler.UpdateUserTimestamp(userDto.UserId, _catcher.GetTimestamp());

            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();
        }
    }
}