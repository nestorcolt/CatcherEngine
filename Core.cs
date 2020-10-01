using System;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)


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

            BlockCatcher catcher = new BlockCatcher();
        }
    }


    class BlockCatcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {
        public BlockCatcher()
        {
            Console.WriteLine("Constructor");
        }


    }

}
