using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CatcherEngine.Modules;
using Newtonsoft.Json.Linq;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace CatcherEngine
{
    class Function
    {
        public BlockCatcher Catcher = new BlockCatcher();
        public BlockValidator Validator = new BlockValidator();

        [Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> CatchHandle(Dictionary<string, string> userData, ILambdaContext context)
        {
            userData.TryGetValue("userId", out string user);

            if (user is null)
                return ">> BadRequest: User Id not present on request Json.";

            // if the user id comes in the invoke will run the code
            string responseCode = await Catcher.GetOffersAsyncHandle(user);

            return $"{responseCode}";

        }

        [Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> ValidatorHandle(Dictionary<string, string> validationData, ILambdaContext context)
        {
            validationData.TryGetValue("userId", out string user);
            validationData.TryGetValue("pickupTime", out string pickUpTimeThreshold);
            validationData.TryGetValue("minimumPrice", out string minimumPrice);
            validationData.TryGetValue("acceptedOffers", out string acceptedOffers);
            validationData.TryGetValue("areas", out string areas);

            if (user is null)
                return ">> BadRequest: User Id not present on request Json.";

            // if the user id comes in the invoke will run the code
            HttpStatusCode responseCode = await Validator.ValidateOffersAsyncHandle(user, pickUpTimeThreshold, minimumPrice, acceptedOffers, areas);

            return $"{responseCode}";

        }
    }
}
