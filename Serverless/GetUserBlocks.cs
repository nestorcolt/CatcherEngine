using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;
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
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(snsEvent.Records[0].Sns.Message);
            bool result = false;

            try
            {
                result = await BlockCatcher.LookingForBlocks(userDto);
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
    }
}