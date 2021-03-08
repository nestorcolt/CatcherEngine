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
        }

        public static async Task DeleteMessage(IAmazonSQS sqsClient, string receiptHandle, string qUrl)
        {
            await sqsClient.DeleteMessageAsync(qUrl, receiptHandle);
        }
    }
}
