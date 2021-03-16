using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;
using System;
using System.Net;
using System.Threading.Tasks;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine.Serverless
{
    class GetUserBlocks
    {
        private ApiHandler Client = new ApiHandler();

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            UserDto userDto = await GetUserDtoAsync(sqsEvent);
            bool recursive = false;

            try
            {
                recursive = await BlockCatcher.LookingForBlocks(userDto);
            }
            catch (Exception e)
            {
                await CloudLogger.Log(e.ToString(), userDto.UserId);
            }
            finally
            {
                // call trigger SQS to call again this lambda (recursion) base on recursion parameter
                if (recursive && userDto.SearchBlocks)
                {
                    // update last iteration value
                    await DynamoHandler.UpdateUserTimestamp(userDto.UserId, BlockCatcher.GetTimestamp());

                    // pass userData SQSEvent
                    string qUrl = await SqsHandler.GetQueueByName(SqsHandler.Client, Constants.PowerTuningQueueName);
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