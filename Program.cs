using System;
using System.Collections.Generic;
using SearchEngine.Properties;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine
{
    class Program
    {
        static void Main(string[] args)

        {
            settings.Default.Debug = true;
            var authenticator = new Authenticator();
            authenticator.Authenticate();

        }
    }
}