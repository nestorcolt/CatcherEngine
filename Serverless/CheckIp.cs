using Amazon.Lambda.Core;
using System;
using System.Collections;
using System.Net;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine.Serverless
{
    class CheckIp
    {

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public string FunctionHandler(IDictionary myEvent, ILambdaContext context)
        {
            string ip = new WebClient().DownloadString("http://checkip.amazonaws.com");
            Console.WriteLine(ip);
            return "OK";
        }
    }
}