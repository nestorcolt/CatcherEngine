using System;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)


namespace FlexCatcher
{
    class Program
    {
        static void Main(string[] args)

        {
            try
            {
                var catcher = new BlockCatcher(userId: "100", flexAppVersion: "3.39.29.0");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }
}
