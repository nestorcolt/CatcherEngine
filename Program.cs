using System;
using Catcher.Properties;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace Catcher
{
    class Program
    {
        static void Main(string[] args)

        {
            settings.Default.Debug = true;
            CatchHandle(args[0]);
        }

        public static void CatchHandle(string userId)
        {
            string[] areas = {

                "221c60b4-2825-4fdf-80de-360cdc73e8f4", "d1b3cfc9-3253-422b-b94a-18f549c231d6", "3d8a7f46-b654-46c0-ad95-69cbaf95560b", "fd409ef8-f297-4543-9b93-6a5e93184313", "724fcdc1-82a8-4cf9-90db-ba6f5b7e246e", "f9530032-4659-4a14-b9e1-19496d97f633", "d39e54d6-1a53-46a9-a22d-309342256ef0", "d98c442b-9688-4427-97b9-59a4313c2f66", "29571892-da88-4089-83f0-24135852c2e4", "5548b40b-1223-4881-87b1-4ae5d4d77f95", "fd440da5-dc81-43bf-9afe-f4910bfd4090", "12bb7066-fac1-4412-83e8-a63247d4a946", "49d080a7-a765-47cf-a29e-0f1842958d4a", "edf6e42c-dd59-4c44-b2af-2811030d3904", "713b178d-f646-4998-998a-b0b9b46801a8", "b90b085e-874f-48da-8150-b0c215efff08", "dd00cb2b-349b-480c-a2d0-5aff2c3fd293", "50ade688-5ae2-48ce-a83c-f0af3fa4a22a", "5540b055-ee3c-4274-9997-de65191d6932", "b403b73b-f289-4a0b-a559-f718274925b3", "e7765dce-8d20-41b6-a038-9856323d4db6", "d263d246-e03a-4fe9-8d8e-fa473aa2958f", "c059f4c8-35bd-4a43-848f-38cb7d9eec9c", "6ff991bc-bc77-4065-9e09-736e571b3143", "ad13a80a-0d93-444c-a660-1b5f65e53626", "81286958-ab75-4c68-925d-ecacd476af30", "21856fd3-11c2-4455-8938-bc4021ee8f6c", "f78af44a-613a-4cea-bfd4-7ad17da2719d"
            };

            string user = userId;
            Console.WriteLine($"Initialize on user: {user}");

            try
            {
                var catcher = new BlockCatcher(user);

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
