using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Catcher.Modules;
using Catcher.Properties;
using Newtonsoft.Json.Linq;

namespace Catcher
{
    class BlockValidator : Engine
    {
        public List<string> Areas;
        public int PickUpTimeThreshold;
        public float MinimumPrice;

        public BlockValidator(string user)
        {
            InitializeEngine(user);
            PickUpTimeThreshold = settings.Default.PickUpTime;
            MinimumPrice = settings.Default.MinimumPrice;

            Areas = new List<string>
            {
                "f9530032-4659-4a14-b9e1-19496d97f633",
                "d98c442b-9688-4427-97b9-59a4313c2f66",
                "29571892-da88-4089-83f0-24135852c2e4",
                "49d080a7-a765-47cf-a29e-0f1842958d4a",
                "fd440da5-dc81-43bf-9afe-f4910bfd4090",
                "b90b085e-874f-48da-8150-b0c215efff08",
                "5540b055-ee3c-4274-9997-de65191d6932",
                "a446e8f9-28fb-4744-ad3f-0098543227ab",
                "033311b9-a6dd-4cfb-b0b7-1b5ee998638b",
                "5eb3af65-0e4e-48d3-99ce-7eff7923c3da",
                "61153cd4-58b5-43bc-83db-bdecf569dcda",
                "8ffc6623-5837-42c0-beea-6ac50ef43faa",
                "7e6dd803-a8a3-4b64-9996-903f88cc5fe7",
                "1496f58f-ca2d-43c7-817b-ec2c3613390d",
                "8cf0c633-504b-4f56-91b1-de1c45ecccb0",
            };
        }

        public async Task<HttpStatusCode> ValidateOffersAsyncHandle(List<string> acceptedOffers)
        // Get all the schedule blocks that the user has on amazon flex account and if one of this match to the acceptedOffers in our register for the day
        // will validate only these blocks. This is because in case the user catch the blocks by hand don't delete those blocks catch out of our tool
        {

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


                    if ((float)offerPrice < MinimumPrice || !Areas.Contains((string)serviceAreaId) || pickUpTimespan < PickUpTimeThreshold)
                    {
                        await ApiHelper.DeleteOfferAsync((int)startTime);
                    }
                }

            }
            return response.StatusCode;

        }

    }
}
