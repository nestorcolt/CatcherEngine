using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.EC2;
using Amazon.EC2.Model;
using SearchEngine.Properties;


namespace SearchEngine
{
    class Authenticator
    {
        private dynamic GetUserData(string userId)

        {
            /*
             * Get the user data making a query to dynamo db table Users parsing the user_id 
             */
            AmazonDynamoDBClient Client = new AmazonDynamoDBClient();
            Table ThreadTable = Table.LoadTable(Client, settings.Default.UsersTable);
            ScanFilter scanFilter = new ScanFilter();
            scanFilter.AddCondition(settings.Default.UserPk, ScanOperator.Equal, userId);

            Search search = ThreadTable.Scan(scanFilter);
            List<dynamic> results = new List<dynamic>();

            do
            {
                var docList = search.GetNextSetAsync();

                docList.Result.ForEach(document =>

                {
                    dynamic attribute = Newtonsoft.Json.JsonConvert.DeserializeObject(document.ToJson());
                    results.Add(attribute);
                });
            } while (!search.IsDone);


            return results[0];
        }

        private string GetEnvironmentVariable()

        {
            string privateIp =
                Environment.GetEnvironmentVariable(settings.Default.IpEnvVar, EnvironmentVariableTarget.User);
            return privateIp;
        }

        public string GetAmazonAccessToken(string refreshToken)
        {
            var authenticationHeader = new Dictionary<string, string>
            {
                {"app_name", "com.amazon.rabbit"},
                {"app_version", "130050002"},
                {"source_token_type", "refresh_token"},
                {"source_token", refreshToken},
                {"requested_token_type", "access_token"}
            };

            return "";
        }

        private string GetUserInstance()
        {
            /*
             * Gets the user instance iterating over all available and if finds one that match with my private IP
             * will get the name tag from it and return this string value which will be something like User-1010 like
             */
            AmazonEC2Client amazonEc2Client = new AmazonEC2Client();
            var response = amazonEc2Client.DescribeInstancesAsync(new DescribeInstancesRequest { });
            string myPrivateIp = GetEnvironmentVariable();
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
            string userInstanceName = GetUserInstance();
            string userId = userInstanceName.Split("-")[1];
            dynamic userData = GetUserData(userId);

            // TODO - continue here. The data is being fetch successfully
            Console.WriteLine(userData);

            var areas = new List<string>();
            string refreshToken = "";
            float minimumPrice = 22.5f;
            float speed = 1.0f;
            int arrivalTime = 30;

            //var catcher = new BlockCatcher(userId, refreshToken, speed, areas, minimumPrice, arrivalTime);
            Console.WriteLine($"Initialize on user: {userId}");

            // Main loop method is being called here
            //catcher.LookingForBlocksLegacy();

        }
    }
}