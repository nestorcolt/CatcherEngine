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
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (isWindows)
            {
                // means that probably im running this from my computer
                settings.Default.Debug = true;
                string myPrivateTestIp = "172.31.0.65";
                Environment.SetEnvironmentVariable(settings.Default.IpEnvVar, myPrivateTestIp, EnvironmentVariableTarget.User);
            }

            // Initialize the search engine
            var auth = new Authenticator();
            bool authState = auth.Authenticate();

            if (authState)
            {
                var catcher = new BlockCatcher(auth.UserId, auth.AccessToken, auth.Speed, auth.Areas, auth.MinimumPrice, auth.ArrivalTime);
                //Console.WriteLine($"Initialize on user: {userId}");

                // Main loop method is being called here
                //catcher.LookingForBlocksLegacy();
            }



        }
    }
}