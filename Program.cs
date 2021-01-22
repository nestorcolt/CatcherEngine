using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using SearchEngine.Properties;
using SearchEngine.Modules;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine
{
    class Program
    {
        static void Main(string[] args)

        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            settings.Default.Version = "Running Version: 22-01-2021 12:50";
            Console.WriteLine(settings.Default.Version);

            if (isWindows)
            {
                // means that probably im running this from my computer. This is the Ec2 private IP
                string myPrivateTestIp = "172.31.6.114";
                Environment.SetEnvironmentVariable(settings.Default.IpEnvVar, myPrivateTestIp, EnvironmentVariableTarget.User);
            }

            BlockCatcher catcher = new BlockCatcher();

            // Main loop method is being called here
            catcher.LookingForBlocksLegacy();
        }

    }

}