using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SearchEngine.Lib;
using SearchEngine.Models;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthenticator _authenticator;

        public AuthenticateUser(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public AuthenticateUser() : this(StartUp.Container.BuildServiceProvider())
        {
            _authenticator = _serviceProvider.GetService<IAuthenticator>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent userData, ILambdaContext context)
        {
            JObject message = JObject.Parse(userData.Records[0].Sns.Message);
            string userId = message["user_id"].ToString();
            string refreshToken = message["refresh_token"].ToString();

            try
            {
                await _authenticator.Authenticate(refreshToken, userId);
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