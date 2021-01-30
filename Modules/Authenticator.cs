using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;

namespace SearchEngine.Modules
{
    class Authenticator
    {
        protected string ErrorSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-ERROR-TOPIC";
        private const string AuthTokenUrl = "https://api.amazon.com/auth/token";
        private const string UserPk = "user_id";
        private string _userId;

        private async Task SetUserData(string userId, string data)
        {
            /*
             * Get the user data making a query to dynamo db table Users parsing the user_id 
             */
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();

            var request = new UpdateItemRequest
            {
                Key = new Dictionary<string, AttributeValue>() { { UserPk, new AttributeValue { S = userId } } },
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    {"#Q", "access_token"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":data",new AttributeValue {S = data}}
                },
                UpdateExpression = "SET #Q = :data",
                TableName = "Users"
            };

            var response = await client.UpdateItemAsync(request);
        }

        private async Task<string> GetAmazonAccessToken(string refreshToken)
        {
            var authenticationHeader = new Dictionary<string, string>
            {
                {"app_name", "com.amazon.rabbit"},
                {"app_version", "130050002"},
                {"source_token_type", "refresh_token"},
                {"source_token", refreshToken},
                {"requested_token_type", "access_token"}
            };

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, AuthTokenUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(authenticationHeader), Encoding.UTF8, "application/json")
            };

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
                return requestToken["access_token"].ToString();
            }

            await ErrorToSnsAsync(_userId);
            throw new UnauthorizedAccessException($"There is a problem with the authentication.\nReason: {response.Content}");
        }

        public async Task ErrorToSnsAsync(string message)
        {
            IAmazonSimpleNotificationService client = new AmazonSimpleNotificationServiceClient();

            var request = new PublishRequest
            {
                TopicArn = ErrorSnsTopic,
                Message = message,
            };

            // send the message
            await client.PublishAsync(request);
        }

        public async Task Authenticate(string refreshToken, string userId)
        {
            _userId = userId;

            // authenticated for new access token
            string accessToken = Task.Run(() => GetAmazonAccessToken(refreshToken)).Result;
            await SetUserData(_userId, accessToken);
        }
    }
}