using System;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)


namespace FlexCatcher
{
    class Program
    {
        static void Main(string[] args)

        {
            string user = "100";
            string flexAppVersion = "3.39.29.0";
            float price = 22.0f;
            int arrivalTime = 0;
            string[] areas = { "29571892-da88-4089-83f0-24135852c2e4" };

            try
            {
                var catcher = new BlockCatcher(userId: user, flexAppVersion: flexAppVersion, minimumPrice: price, pickUpTimeThreshold: arrivalTime, areas: areas);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }
}
