using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;
using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine.Serverless
{
    class GetUserBlocks
    {
        private ApiHandler Client = new ApiHandler();

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            UserDto userDto = await GetUserDtoAsync(snsEvent);

            try
            {
                await BlockCatcher.LookingForBlocks(userDto);
            }
            catch (Exception e)
            {
                await CloudLogger.Log(e.ToString(), userDto.UserId);
            }

            string ip = new WebClient().DownloadString("http://checkip.amazonaws.com");
            Console.WriteLine($"User: {userDto.UserId} with IP: {ip}");

            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();
        }

        private async Task<UserDto> GetUserDtoAsync(SNSEvent snsEvent)
        {
            JObject oldData = JObject.Parse(snsEvent.Records[0].Sns.Message);
            string newData = await DynamoHandler.QueryUser(oldData["user_id"].ToString());
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(newData);
            return userDto;
        }
    }
}