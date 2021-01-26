using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using SearchEngine.Properties;

namespace SearchEngine.Modules
{
    static class CloudLogger
    {
        public static string LogToCloudTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-LOGS-SERVICE";
        public static int SendMessageInSecondsThreshold = 60;
        public static int SecondsCounter;

        public static async Task LogToSnsAsync(string message, string subject)
        {
            IAmazonSimpleNotificationService client = new AmazonSimpleNotificationServiceClient();

            var request = new PublishRequest
            {
                TopicArn = LogToCloudTopic,
                Message = message,
                Subject = subject,
            };

            // send the message
            await client.PublishAsync(request);

        }
    }
}