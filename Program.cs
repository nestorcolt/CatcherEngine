using SearchEngine.Properties;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace SearchEngine
{
    class Program
    {
        static void Main(string[] args)

        {
            settings.Default.Version = "Running Version: 22-01-2021 14:26";
            settings.Default.UserId = "12";

            // Instance the search engine class
            BlockCatcher catcher = new BlockCatcher();

            // Main loop method is being called here
            catcher.LookingForBlocksLegacy();
        }
    }
}