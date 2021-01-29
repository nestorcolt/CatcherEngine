using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace SearchEngine.Modules
{
    static class CloudLogger
    {
        // TODO REMOVE THIS ACCOUNT ID
        private const string LogToCloudTopic = "arn:aws:sns:us-east-1:436783151981:SE-LOGS-TOPIC";
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