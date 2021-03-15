using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace SearchEngine.Modules
{
    static class SnsHandler
    {
        public static async Task PublishToSnsAsync(string message, string subject, string topicArn)
        {
            IAmazonSimpleNotificationService client = new AmazonSimpleNotificationServiceClient();

            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = message,
                Subject = subject,
            };

            // send the message
            await client.PublishAsync(request);

        }
    }
}
