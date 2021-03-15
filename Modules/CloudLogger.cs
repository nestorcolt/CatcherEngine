using System;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using SearchEngine.Properties;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    static class CloudLogger
    {
        public static string LogToCloudTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-LOGS-TOPIC";
        public static string UserLogStreamName = "User-{0}";

        public static async Task Log(string message, string userId)
        {
            await SnsHandler.PublishToSnsAsync(message, String.Format(UserLogStreamName, userId), LogToCloudTopic);
        }
    }
}