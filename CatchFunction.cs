using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Catcher.Modules;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

namespace Catcher
{
    class CatchFunction
    {
        public BlockCatcher Catcher = new BlockCatcher();

        public async Task<string> CatchHandle(string userId, ILambdaContext context)
        {
            try
            {
                await Catcher.GetOffersAsyncHandle(userId);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return "My result here!";

        }
    }
}
