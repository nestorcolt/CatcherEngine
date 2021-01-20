using System;
using System.Runtime.InteropServices;
using SearchEngine.Properties;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine
{
    class Program
    {
        static void Main(string[] args)

        {
            Console.WriteLine($"Running Version: 18-01-2021 17:25");
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            settings.Default.Debug = true;

            if (isWindows)
            {
                // means that probably im running this from my computer
                string myPrivateTestIp = "172.31.7.5";
                Environment.SetEnvironmentVariable(settings.Default.IpEnvVar, myPrivateTestIp, EnvironmentVariableTarget.User);
            }

            BlockCatcher catcher = new BlockCatcher();

            // Main loop method is being called here
            //catcher.LookingForBlocksLegacy();
        }

    }

}