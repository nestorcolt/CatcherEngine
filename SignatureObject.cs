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

        public static string Url = "https://aws-url-host.com/v1/reports/MyService?EndDateTime=2020-01-13&Siteid=63&StartDateTime=2019-09-20";
        public static string XApiKey = "APIKEY-TOCALLAWSFUNCTION-IFNEEDED";
        public static string SecretKey = "YOURAWS-SECRETKEY-INHERE";
        public static string AwsServiceName = "execute-api";
        public static string AwsRegion = "eu-west-2";

        public static string CreateSignature()
        {
            // 0. Prepare request message.
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, Url);
            msg.Headers.Host = msg.RequestUri.Host;

            // Get and save dates ready for further use.
            DateTimeOffset utcNowSaved = DateTimeOffset.UtcNow;
            string amzLongDate = utcNowSaved.ToString("yyyyMMddTHHmmssZ");
            string amzShortDate = utcNowSaved.ToString("yyyyMMdd");

            // Add to headers. 
            msg.Headers.Add("x-amz-date", amzLongDate);
            msg.Headers.Add("x-api-key", XApiKey); // My API call needs an x-api-key passing also for function security.


            // **************************************************** SIGNING PORTION ****************************************************
            // 1. Create Canonical Request            
            var canonicalRequest = new StringBuilder();
            canonicalRequest.Append(msg.Method + "\n");
            canonicalRequest.Append(string.Join("/", msg.RequestUri.AbsolutePath.Split('/').Select(Uri.EscapeDataString)) + "\n");
            canonicalRequest.Append(GetCanonicalQueryParams(msg) + "\n"); // Query params to do.

            var headersToBeSigned = new List<string>();

            foreach (var header in msg.Headers.OrderBy(a => a.Key.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
            {
                canonicalRequest.Append(header.Key.ToLowerInvariant());
                canonicalRequest.Append(":");
                canonicalRequest.Append(string.Join(",", header.Value.Select(s => s.Trim())));
                canonicalRequest.Append("\n");
                headersToBeSigned.Add(header.Key.ToLowerInvariant());
            }

            canonicalRequest.Append("\n");
            var signedHeaders = string.Join(";", headersToBeSigned);

            canonicalRequest.Append(signedHeaders + "\n");
            canonicalRequest.Append("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"); // Signature for empty body.

            // 2. String to sign.            
            string stringToSign = "AWS4-HMAC-SHA256" + "\n" + amzLongDate + "\n" + amzShortDate + "/" + AwsRegion + "/" + AwsServiceName + "/aws4_request" + "\n" + Hash(Encoding.UTF8.GetBytes(canonicalRequest.ToString()));

            // 3. Signature with compounded elements.
            var dateKey = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + SecretKey), amzShortDate);
            var dateRegionKey = HmacSha256(dateKey, AwsRegion);
            var dateRegionServiceKey = HmacSha256(dateRegionKey, AwsServiceName);
            var signingKey = HmacSha256(dateRegionServiceKey, "aws4_request");

            var signature = ToHexString(HmacSha256(signingKey, stringToSign.ToString()));
            return signature;

            // **************************************************** END SIGNING PORTION ****************************************************

        }


        // --------------------------------- Utilities ----------------------------------------------------------------

        private static string GetCanonicalQueryParams(HttpRequestMessage request)
        {
            var values = new SortedDictionary<string, string>();
            var querystring = HttpUtility.ParseQueryString(request.RequestUri.Query);

            foreach (var key in querystring.AllKeys)
            {
                if (key == null)//Handles keys without values
                {
                    values.Add(Uri.EscapeDataString(querystring[key]), $"{Uri.EscapeDataString(querystring[key])}=");
                }
                else
                {
                    // Escape to upper case. Required.
                    values.Add(Uri.EscapeDataString(key), $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(querystring[key])}");
                }
            }

            // Put in order - this is important.
            var queryParams = values.Select(a => a.Value);
            return string.Join("&", queryParams);
        }

        private static string ToHexString(IReadOnlyCollection<byte> array)
        {
            var hex = new StringBuilder(array.Count * 2);
            foreach (var b in array)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            return new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        public static string Hash(byte[] bytesToHash)
        {
            return ToHexString(SHA256.Create().ComputeHash(bytesToHash));
        }
    }

}