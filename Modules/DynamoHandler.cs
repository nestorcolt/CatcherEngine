using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    static class DynamoHandler
    {

        public static string QueryUser(string userId)
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();

            QueryFilter scanFilter = new QueryFilter();
            Table usersTable = Table.LoadTable(client, "Users");
            scanFilter.AddCondition("user_id", ScanOperator.Equal, userId);

            Search search = usersTable.Query(scanFilter);
            List<Document> documentSet = search.GetNextSetAsync().Result;

            if (documentSet.Count > 0)
            {
                return documentSet[0].ToJson();
            }

            return null;

        }

    }
}
