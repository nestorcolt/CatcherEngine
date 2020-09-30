using System;

/// <summary>
///     The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
///     to be run per user request. (Ideally on a Lambda funciton over the AWS architecture)
/// </summary>

namespace AmazonFlexServices
{
    class Core
    {
        static void Main(string[] args)
        {

            string serviceAreaURL = "https://flex-capacity-na.amazon.com/eligibleServiceAreas";
            string ownerEndpointURL = "https://" + args[0] + "/admin/script_functions.php";
            string offersURL = "https://flex-capacity-na.amazon.com/GetOffersForProviderPost";
            string acceptURL = "https://flex-capacity-na.amazon.com/AcceptOffer";

            Console.WriteLine(ownerEndpointURL);

            BlockCatcher instance = new BlockCatcher();
            instance.Run();
        }
    }


    class BlockCatcher

        /// The Engine of the program. Will look for available blocks depending on the parsed data, makin api calls to amazon to check for for blocks to pick up by drivers.
        /// Will used asynchronous programming and multithreading to speed up the process and the api request.

    {
        public void Run()
        {
            Console.WriteLine("Init Method");
        }

    }
}
