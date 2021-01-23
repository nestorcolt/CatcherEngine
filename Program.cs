using SearchEngine.Modules;
using SearchEngine.Properties;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine
{
    class Program
    {
        static void Main(string[] args)

        {
            settings.Default.Version = "Running Version: 23-01-2021 12:00";
            settings.Default.UserId = "11";

            Authenticator authenticator = new Authenticator();
            authenticator.Authenticate();

            //Instance the search engine class
            BlockCatcher catcher = new BlockCatcher(authenticator);

            // Main loop method is being called here
            catcher.LookingForBlocksLegacy();
        }
    }
}