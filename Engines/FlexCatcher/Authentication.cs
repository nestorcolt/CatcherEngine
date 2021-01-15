using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;

namespace SearchEngine.Engines.FlexCatcher
{
    public class Authentication
    {
        public const string AuthTemplatePath = @".\\Json\AuthTemplate.json";
        public const string AuthTokenPath = @".\\Json\AuthToken.json";
        public const string RefreshTokenPath = @".\\Json\RefreshAuthHeader.json";

        private dynamic _authTemplate;
        public string RefreshToken;
        public string AccessToken;
        public string UserEmail { get; set; }
        public string UserPassword { get; set; }

        public Authentication()
        {
            // Init api helper class
            ApiHandle.InitializeClient();
        }

        public async Task<string> GetTokenData()
        {
            JObject tokenTemplate = await StreamHandle.GetFromJsonAsync(AuthTokenPath);
            JToken time = tokenTemplate["time"];

            if (time.Type != JTokenType.Null)
            {
                long unixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                long tokenTime = long.Parse(time.ToString());
                return tokenTemplate["refresh_token"].ToString();
                //if (unixTime < (tokenTime + 36000)) // iF CURRENT TIME IS LESS THAN TOKEN TIME + 1 HOUR (TOKEN EXPIRES 36000 SECS)
                //{
                //    return tokenTemplate["refresh_token"].ToString();
                //}
            }

            // null if there is no time span or the current time is grater than 1 hour (TOKEN EXPIRED)
            return null;
        }

        public async Task ParseUserCredentials(string user, string password, bool forceNew, string otp = null)
        {
            string token = await GetTokenData();

            UserEmail = user;
            UserPassword = password;

            if (token != null && forceNew is false)
            {
                _authTemplate = TokenToTemplate(token);
            }
            else
            {
                _authTemplate = CredentialsToTemplate();
            }
        }

        public JObject GetAuthTemplate()
        /*
            Get an authentication template and return this json token
         */
        {
            // This will be used later to test more templates
            Random random = new Random();
            int index = random.Next(1, 1000);
            JObject jsonTemplate = StreamHandle.GetFromJsonAsync(AuthTemplatePath).Result;
            return jsonTemplate;
        }

        public JObject CredentialsToTemplate(string topCode = null)
        /*
            Parse the user Flex Credentials to the template. The first step of the authentication
         */
        {
            JObject authTemplate = GetAuthTemplate();
            string password = topCode ?? UserPassword;

            authTemplate["auth_data"]["user_id_password"]["user_id"] = UserEmail;
            authTemplate["auth_data"]["user_id_password"]["password"] = password;
            return authTemplate;
        }

        public JObject TokenToTemplate(string token)
        /*
            Parse the token to the template. This is only used after parsing the OTP
        */
        {
            JObject authTemplate = GetAuthTemplate();
            authTemplate.Remove("auth_data");

            JObject authJObject = new JObject(
                new JProperty("use_global_authentication", "true"),
                new JProperty("access_token", token)
                );

            authTemplate["auth_data"] = authJObject;
            return authTemplate;
        }

        public async Task SaveTokenData(string refreshToken, string time)
        {
            var tokenData = new Dictionary<string, string>
            {
                {"refresh_token", refreshToken},
                {"time", time}
            };

            await StreamHandle.SaveJsonAsync(AuthTokenPath, tokenData);
        }

        public async Task RefreshAccessToken()
        {
            var jsonTemplate = StreamHandle.GetFromJsonAsync(RefreshTokenPath).Result.ToObject<IDictionary<string, string>>();
            using var newContent = new FormUrlEncodedContent(jsonTemplate);

            newContent.Headers.Clear();
            newContent.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            HttpResponseMessage response = await ApiHandle.ApiClient.PostAsync(ApiHandle.ApiRefreshUrl, newContent);
            JObject requestToken = await ApiHandle.GetRequestJTokenAsync(response);
            Console.WriteLine(requestToken);
        }

        public async Task AuthenticateUser()
        {
            // Http request to get the response header
            HttpResponseMessage content = await ApiHandle.PostDataAsync(ApiHandle.ApiAuthUrl, _authTemplate.ToString(), ApiHandle.ApiClient);
            JObject requestToken = await ApiHandle.GetRequestJTokenAsync(content);
            JObject response = requestToken.SelectToken("response").ToObject<JObject>();

            if (content.StatusCode == HttpStatusCode.OK)
            {
                // Check for success token and return to class properties the access token
                JToken success = response["success"];

                if (success != null)
                {
                    JToken tokens = success["tokens"]["bearer"];
                    AccessToken = tokens["access_token"].ToString();
                    RefreshToken = tokens["refresh_token"].ToString();
                    Console.WriteLine(AccessToken);
                    string unixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString();


                    //JObject oldToken = _authTemplate["auth_data"]["access_token"];

                    //if (oldToken == null)
                    //await SaveTokenData(RefreshToken, unixTime);

                    Console.WriteLine(" ***** Authentication Successful ********");
                    return;
                }
            }
            else
            {
                // Check if there is a challenge authentication type
                JToken challenge = response["challenge"];
                JToken error = response["error"];

                if (challenge != null)
                {
                    Console.WriteLine($"\nChallenge Reason: {challenge["challenge_reason"]}\n");
                    return;
                }

                if (error != null)
                {
                    Console.WriteLine($"\nError Reason: {error["message"]}\n");
                    return;
                }

            }

        }

    }
}