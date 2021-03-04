using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace SearchEngine.Modules
{
    static class SqsHandler
    {
        public static IAmazonSQS Client = new AmazonSQSClient();
        public static string StartSearchQueueName = @"GetUserBlocksQueue";

        public static async Task<string> GetQueueByName(IAmazonSQS sqsClient, string name)
        {
            ListQueuesResponse responseList = await sqsClient.ListQueuesAsync(name);

            if (responseList.QueueUrls.Count > 0)
            {
                return responseList.QueueUrls[0];
            }

            return null;
        }

        public static async Task SendMessage(IAmazonSQS sqsClient, string qUrl, string messageBody)
        {
            SendMessageResponse responseSendMsg = await sqsClient.SendMessageAsync(qUrl, messageBody);
            Console.WriteLine($"Message added to queue\n  {qUrl}");
            Console.WriteLine($"HttpStatusCode: {responseSendMsg.HttpStatusCode}");
        }
        public static async Task DeleteMessage(IAmazonSQS sqsClient, Message message, string qUrl)
        {
            Console.WriteLine($"\nDeleting message {message.MessageId} from queue...");
            await sqsClient.DeleteMessageAsync(qUrl, message.ReceiptHandle);
        }
    }
}
