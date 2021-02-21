using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace SearchEngine.Modules
{
    class DynamoHandle
    {
        private const string UserPk = "user_id";
        private const string UsersTable = "Users";

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
                    {":data", new AttributeValue {S = data}}
                },
                UpdateExpression = "SET #Q = :data",
                TableName = UsersTable
            };

            var response = await client.UpdateItemAsync(request);
        }
    }
}