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


            var areas = new List<string>
            {
                "f9530032-4659-4a14-b9e1-19496d97f633",
                "d98c442b-9688-4427-97b9-59a4313c2f66",
            };

            string user = "1111";
            string accessToken = "";
            float minimumPrice = 22.5f;
            float speed = 1.0f;
            int arrivalTime = 30;


            try
            {
                var catcher = new BlockCatcher(user, accessToken, speed, areas, minimumPrice, arrivalTime);
                Console.WriteLine($"Initialize on user: {user}");

                // Main loop method is being called here
                Console.WriteLine("Looking for blocks 3, 2, 1 ...");
                //catcher.LookingForBlocksLegacy();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}