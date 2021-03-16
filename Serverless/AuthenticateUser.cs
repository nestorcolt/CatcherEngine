using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;
using System;
using System.Net;
using System.Threading.Tasks;

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

            try
            {
                await Authenticator.Authenticate(refreshToken, userId);
            }
            catch (Exception e)
            {
                await CloudLogger.Log(e.ToString(), userId);
            }

            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();
        }
    }
}