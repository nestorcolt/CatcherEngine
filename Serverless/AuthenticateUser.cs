using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)


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

            Authenticator authenticate = new Authenticator();
            await authenticate.Authenticate(refreshToken, userId);

            HttpStatusCode responseCode = HttpStatusCode.Accepted;
            return responseCode.ToString();

        }
    }
}