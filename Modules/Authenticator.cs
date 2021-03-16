using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    static class Authenticator
    {
        public static async Task<string> GetAmazonAccessToken(string refreshToken, string userId)
        {
            var authenticationHeader = new Dictionary<string, string>
            {
                {"app_name", "com.amazon.rabbit"},
                {"app_version", settings.Default.FlexAppVersion.Replace(".", "")},
                {"source_token_type", "refresh_token"},
                {"source_token", refreshToken},
                {"requested_token_type", "access_token"}
            };

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, Constants.AuthTokenUrl)
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

            await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(Constants.UserPk, userId)).ToString(), "msg", Constants.ErrorSnsTopic);
            throw new UnauthorizedAccessException($"There is a problem with the authentication.\nReason: {response.Content}");
        }

        public static async Task Authenticate(string refreshToken, string userId)
        {
            // authenticated for new access token
            string accessToken = Task.Run(() => GetAmazonAccessToken(refreshToken, userId)).Result;
            await DynamoHandler.SetUserData(userId, accessToken);
        }
    }
}