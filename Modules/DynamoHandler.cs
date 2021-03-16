﻿using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    static class DynamoHandler
    {
        private static readonly AmazonDynamoDBClient Client = new AmazonDynamoDBClient();
        private static string _lastIteration = "last_iteration";
        private static string _accessToken = "access_token";
        private static string _serviceArea = "service_area";
        private static string _tableName = "Users";
        private static string _tablePk = "user_id";

        public static async Task UpdateUserTimestamp(string userId, int timeStamp)
        {
            Table table = Table.LoadTable(Client, _tableName);
            Document document = new Document { [_lastIteration] = timeStamp };
            await table.UpdateItemAsync(document, userId);
        }

        public static async Task<string> QueryUser(string userId)
        {
            QueryFilter scanFilter = new QueryFilter();
            Table usersTable = Table.LoadTable(Client, _tableName);
            scanFilter.AddCondition(_tablePk, ScanOperator.Equal, userId);

            Search search = usersTable.Query(scanFilter);
            List<Document> documentSet = await search.GetNextSetAsync();

            if (documentSet.Count > 0)
            {
                return documentSet[0].ToJson();
            }

            return null;
        }

        public static async Task SetUserData(string userId, string accessToken, string serviceArea)
        {
            Table usersTable = Table.LoadTable(Client, _tableName);

            Document order = new Document
            {
                [_serviceArea] = serviceArea,
                [_accessToken] = accessToken
            };

            await usersTable.UpdateItemAsync(order, userId);
        }
    }
}