using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    static class CloudLogger
    {
        private const string LogToCloudTopic = "arn:aws:sns:us-east-1:320132171574:SE-LOGS-SERVICE";

        public static async Task LogToSnsAsync(string message, string subject)
        {
            IAmazonSimpleNotificationService client = new AmazonSimpleNotificationServiceClient();

            var request = new PublishRequest
            {
                TopicArn = LogToCloudTopic,
                Message = message,
                Subject = subject,
            };

            await client.PublishAsync(request);
        }
    }
}