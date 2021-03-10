using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;

namespace SearchEngine.Serverless
{
    class GetUserBlocksTest
    {

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {

            BlockCatcher catcher = new BlockCatcher();
            UserDto userDto = await GetUserDtoAsync(sqsEvent);
            bool recursive = false;

            try
            {
                catcher.BlockCatcherInit(userDto);
                recursive = catcher.LookingForBlocks();
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
                    // update last iteration value
                    await DynamoHandler.UpdateUserTimestamp(userDto.UserId, catcher.GetTimestamp());

                    // pass userData SQSEvent
                    string qUrl = await SqsHandler.GetQueueByName(SqsHandler.Client, SqsHandler.StartSearchQueueName);
                    await SqsHandler.SendMessage(SqsHandler.Client, qUrl, sqsEvent.Records[0].Body);
                    await SqsHandler.DeleteMessage(SqsHandler.Client, sqsEvent.Records[0].ReceiptHandle, qUrl);
                }
            }

            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();
        }

        private async Task<UserDto> GetUserDtoAsync(SQSEvent sqsEvent)
        {
            JObject oldData = JObject.Parse(sqsEvent.Records[0].Body);
            string newData = await DynamoHandler.QueryUser(oldData["user_id"].ToString());
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(newData);
            return userDto;
        }
    }
}
