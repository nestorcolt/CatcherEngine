using SearchEngine.Properties;

namespace SearchEngine.Modules
{
    static class Constants
    {
        public static string AuthenticationSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-AUTHENTICATE-TOPIC";
        public static string AcceptedSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-ACCEPTED-TOPIC";
        public static string OffersSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-OFFERS-TOPIC";
        public static string LogToCloudTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-LOGS-TOPIC";
        public static string ErrorSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-ERROR-TOPIC";
        public static string SleepSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-SLEEP-TOPIC";
        public static string StopSnsTopic = $"arn:aws:sns:us-east-1:{settings.Default.AWSAccountId}:SE-STOP-TOPIC";

        // main URLS
        public const string AcceptInputUrl = "http://internal.amazon.com/coral/com.amazon.omwbuseyservice.offers/";
        public const string AuthTokenUrl = "https://api.amazon.com/auth/token";
        public const string ApiBaseUrl = "https://flex-capacity-na.amazon.com/";

        // paths
        public static string AcceptUri = "AcceptOffer";
        public static string OffersUri = "GetOffersForProviderPost";
        public static string ServiceAreaUri = "eligibleServiceAreas";

        public static string AppVersion => settings.Default.FlexAppVersion;

        public const string UserPk = "user_id";
        public static string UserLogStreamName = "User-{0}";
        public const string TokenKeyConstant = "x-amz-access-token";

        // SQS
        public static string StartSearchQueueName = @"GetUserBlocksQueue";
        public static string PowerTuningQueueName = @"PowerTunningQueue";
    }
}
