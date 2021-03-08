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

        public static async Task PublishToSnsAsync(string message, string subject, string topicArn = null)
        {
            IAmazonSimpleNotificationService client = new AmazonSimpleNotificationServiceClient();
            string topic = topicArn == null ? LogToCloudTopic : topicArn;

            var request = new PublishRequest
            {
                TopicArn = topic,
                Message = message,
                Subject = subject,
            };

            // send the message
            await client.PublishAsync(request);

        }
    }
}