using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;
using SearchEngine.Properties;
using SearchEngine.Modules;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine.Serverless
{
    class GetUserBlocks
    {
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent userData, ILambdaContext context)
        {
            settings.Default.Version = "Running Version: 26-01-2021 23:49";
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(userData.Records[0].Sns.Message);
            string logUserId = $"User-{userDto.UserId}";

            try
            {
                BlockCatcher catcher = new BlockCatcher(userDto);
                await catcher.LookingForBlocksLegacy();
            }
            catch (Exception e)
            {
                await CloudLogger.LogToSnsAsync(message: e.ToString(), subject: logUserId);
            }

            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();
        }
    }
}