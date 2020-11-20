using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Catcher.Modules;
using Newtonsoft.Json.Linq;

namespace CatcherEngine.Modules
{
    class BlockValidator : Engine
    {
        public BlockValidator(string user)
        {
            InitializeEngine(userId: user);
        }
        public async Task<HttpStatusCode> ValidateOffersAsyncHandle(List<string> acceptedOffers)
        // Get all the schedule blocks that the user has on amazon flex account and if one of this match to the acceptedOffers in our register for the day
        // will validate only these blocks. This is because in case the user catch the blocks by hand don't delete those blocks catch out of our tool
        {
            var areas = new List<string>();
            int pickUpTimeThreshold = 0;
            float minimumPrice = 0;

            // if the validation is not success will try to find in the catch blocks the one did not passed the validation and forfeit them
            var response = await GetBlockFromDataBaseAsync(ApiHelper.AssignedBlocks);
            JObject blocksArray = await ApiHelper.GetRequestJTokenAsync(response);

            if (!blocksArray.HasValues)
                return response.StatusCode;

            foreach (var block in blocksArray.Values())
            {
                if (!block.HasValues)
                    continue;

                JToken innerBlock = block[0];
                JToken startTime = innerBlock["startTime"];

                if (acceptedOffers.Contains(startTime.ToString()))
                {
                    JToken serviceAreaId = innerBlock["serviceAreaId"];
                    JToken offerPrice = innerBlock["bookedPrice"]["amount"];

                    // The time the offer will be available for pick up at the facility
                    int pickUpTimespan = (int)startTime - GetTimestamp();


                    if ((float)offerPrice < minimumPrice || !areas.Contains((string)serviceAreaId) || pickUpTimespan < pickUpTimeThreshold)
                    {
                        await ApiHelper.DeleteOfferAsync((int)startTime);
                    }
                }

            }
            return response.StatusCode;

        }

    }
}
