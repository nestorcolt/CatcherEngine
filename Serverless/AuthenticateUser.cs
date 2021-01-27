using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;

/*
 * The authenticator is in charge of getting the new access token every time is necessary
 * for instance when the current token has expired.
 */

namespace SearchEngine.Serverless
{
    class AuthenticateUser
    {
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent userData, ILambdaContext context)
        {
            JObject message = JObject.Parse(userData.Records[0].Sns.Message);
            string userId = message["user_id"].ToString();
            string refreshToken = message["refresh_token"].ToString();
            string logUserId = $"User-{userId}";

            try
            {
                Authenticator authenticate = new Authenticator();
                await authenticate.Authenticate(refreshToken, userId);
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