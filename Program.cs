using System;
using System.IO;
using Catcher.Properties;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace Catcher
{
    class Program
    {
        static void Main(string[] args)

        {

            /* args[]:
             * 0 - user ID
             * 1 - price
             * 2 - pick up time
             *
             */

            settings.Default.Debug = true;
            settings.Default.PickUpTime = int.Parse(args[2]);
            settings.Default.MinimumPrice = float.Parse(args[1]);
            CatchHandle(args[0]);
        }

        public static void CatchHandle(string userId)
        {

            string user = userId;
            Console.WriteLine($"Initialize on user: {user}");

            try
            {
                var catcher = new BlockCatcher(user);

                // Main loop method is being called here
                Console.WriteLine("Looking for blocks 3, 2, 1 ...");
                catcher.LookingForBlocksLegacy();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
    }
}
