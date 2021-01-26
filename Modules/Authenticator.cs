﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    class Authenticator
    {
        private const string AuthTokenUrl = "https://api.amazon.com/auth/token";
        private const string UserPk = "user_id";

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

        private static async Task<string> GetAmazonAccessToken(string refreshToken)
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

            throw UnauthorizedAccessException(response);
        }

        private static Exception UnauthorizedAccessException(HttpResponseMessage response)
        {
            // TODO probably adding code to handle the error. HINT: Send SNS message to topic to track this.
            throw new UnauthorizedAccessException($"There is a problem with the authentication.\nReason: {response.Content}");
        }

        public async Task Authenticate(string refreshToken, string userId)
        {
            // authenticated for new access token
            string accessToken = Task.Run(() => GetAmazonAccessToken(refreshToken)).Result;
            await SetUserData(userId, accessToken);
        }
    }
}