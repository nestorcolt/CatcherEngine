using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace FlexCatcher
{

    public static class SignatureObject
    {
        private const string SignaturePrefix = "RABBIT3-HMAC-SHA256";

        public static string CreateSignature(string url, string token)
        {
            // 0. Prepare request message.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

            string amzLongDate = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ");
            string canonicalUrl = request.RequestUri.AbsolutePath;
            string requestId = Guid.NewGuid().ToString();
            string hostUrl = request.RequestUri.Host;

            var headers = new SortedDictionary<string, string>()

            {
                ["host"] = hostUrl,
                ["x-amz-access-token"] = token,
                ["X-Amz-RequestId"] = requestId,
                ["X-Amz-Date"] = amzLongDate
            };


            // **************************************************** SIGNING PORTION ****************************************************

            var canonicalRequest = GetCanonicalRequest(headers, canonicalUrl);
            string stringToSign = GetStringToSign(canonicalRequest[0], amzLongDate);
            var secret = token.Reverse();

            var sequence = new List<string>() { "RABBIT" + secret, amzLongDate.Substring(0, 8), "rabbit_request", stringToSign };
            var signedHexString = SignSequenceString(sequence);

            string authHeader = $"{SignaturePrefix} SignedHeaders={canonicalRequest[1]},Signature={signedHexString}";
            return authHeader;

            // **************************************************** END SIGNING PORTION ****************************************************

        }


        // --------------------------------- Utilities ----------------------------------------------------------------

        private static List<string> GetCanonicalRequest(SortedDictionary<string, string> headers, string canonicalPath)
        {
            string canonicalHeaderString = "";

            foreach (var key in headers)
            {
                canonicalHeaderString += key.Key.ToLower() + ":" + key.Value.ToString() + "\n";
            }

            // Put in order - this is important.
            var queryParams = headers.Select(header => header.Key.ToLower());
            string signHeader = string.Join(";", queryParams);
            var stringRequest = $"POST\n{canonicalPath}\n{canonicalHeaderString}\n{signHeader}";
            var returnInfo = new List<string>() { stringRequest, signHeader };
            return returnInfo;
        }


        private static string SignSequenceString(List<string> inputStringSequence)
        {

            dynamic key = null;

            foreach (var row in inputStringSequence)
            {
                if (key == null)
                {
                    byte[] bytes = Convert.FromBase64String(row);
                    key = Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    byte[] bytes = Convert.FromBase64String(row);
                    var data = Encoding.UTF8.GetString(bytes);
                    key = HmacSha256(key, data);
                }
            }

            var hexKey = ToHex(key, false);
            return hexKey;
        }

        private static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            foreach (var n in bytes)
                result.Append(n.ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        private static string SHA256HexHashString(string stringIn)
        {
            string hashString;

            using (var sha256 = SHA256Managed.Create())
            {
                var hash = sha256.ComputeHash(Encoding.Default.GetBytes(stringIn));
                hashString = ToHex(hash, false);
            }

            return hashString;

        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            return new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string GetStringToSign(string canonicalRequest, string time)
        {
            return $"{SignaturePrefix}\n{time}\n{SHA256HexHashString(canonicalRequest)}";
        }
    }

}