using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace SearchEngine.Lib
{
    static class StsHandler
    {
        private static readonly AmazonSecurityTokenServiceClient Client = new AmazonSecurityTokenServiceClient();

        public static string GetAccountId()
        {
            var getCallerIdentityResponse = Client.GetCallerIdentityAsync(new GetCallerIdentityRequest()).Result;
            return getCallerIdentityResponse.Account;
        }
    }
}
