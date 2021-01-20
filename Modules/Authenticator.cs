using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.EC2;
using Amazon.EC2.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;

namespace SearchEngine.Modules
{
    class Authenticator
    {
        private const string AuthTokenUrl = "https://api.amazon.com/auth/token";
        public string UserId;
        public string RefreshToken;
        public string AccessToken;
        public float MinimumPrice;
        public float Speed;
        public List<string> Areas;
        public List<JToken> SearchSchedule;

        private dynamic GetUserData(string userId)

        {
            /*
             * Get the user data making a query to dynamo db table Users parsing the user_id 
             */
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            ScanFilter scanFilter = new ScanFilter();
            Table usersTable = Table.LoadTable(client, settings.Default.UsersTable);
            scanFilter.AddCondition(settings.Default.UserPk, ScanOperator.Equal, userId);

            Search search = usersTable.Scan(scanFilter);
            List<dynamic> results = new List<dynamic>();

            do
            {
                var docList = search.GetNextSetAsync();

                docList.Result.ForEach(document =>

                {
                    dynamic attribute = JsonConvert.DeserializeObject(document.ToJson());
                    results.Add(attribute);
                });
            } while (!search.IsDone);

            if (results.Count == 0)
            {
                var exception = new AmazonDynamoDBException($"ERROR: Not user found in 'Users Table' with index {userId}.\nAborting ...\n");
                Console.WriteLine(exception.Message);
                Environment.Exit(0);
            }

            return results[0];
        }


        public static async Task<string> GetAmazonAccessToken(string refreshToken)
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

        private string GetEnvironmentVariable()

        {
            string privateIp = Environment.GetEnvironmentVariable(settings.Default.IpEnvVar, EnvironmentVariableTarget.User);
            return privateIp;
        }

        public string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {

                    if (isWindows)
                    {
                        return GetEnvironmentVariable();
                    }

                    return ip.ToString();
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private string GetUserInstance()
        {
            /*
             * Gets the user instance iterating over all available and if finds one that match with my private IP
             * will get the name tag from it and return this string value which will be something like User-1010 like
             */
            AmazonEC2Client amazonEc2Client = new AmazonEC2Client();
            var response = amazonEc2Client.DescribeInstancesAsync(new DescribeInstancesRequest { });
            string myPrivateIp = GetLocalIpAddress();
            string instanceName = null;

            foreach (var instance in response.Result.Reservations)
            {
                string privateIp = instance.Instances[0].PrivateIpAddress;

                if (myPrivateIp == privateIp)
                {
                    instanceName = instance.Instances[0].Tags.Find(x => x.Key == "Name").Value;
                    break;
                }
            }

            return instanceName;
        }


        public void Authenticate()
        {
            // Get the user instance name through the private IP matching these in the available ec2 on account
            string userInstanceName = GetUserInstance();
            UserId = userInstanceName.Split("-")[1];

            // User data collected from dynamo DB 
            dynamic userData = GetUserData(UserId);

            SearchSchedule = userData["search_schedule"].ToObject<List<JToken>>();
            Areas = userData["areas"].ToObject<List<string>>();
            RefreshToken = userData["refresh_token"];
            MinimumPrice = userData["minimum_price"];
            Speed = userData["speed"];

            // authenticated for new access token
            AccessToken = Task.Run(() => GetAmazonAccessToken(RefreshToken)).Result;

        }
    }
}