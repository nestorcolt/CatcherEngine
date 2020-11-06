using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FlexCatcher
{
    class ValidateOffers : BlockCatcher
    {

        public async Task ValidateOffersAsync(string token)

        {
            var areas = new List<string>();
            int pickUpTimeThreshold = 0;
            float minimumPrice = 0;

            // if the validation is not success will try to find in the catch blocks the one did not passed the validation and forfeit them
            var response = await ApiHelper.GetBlockFromDataBaseAsync(ApiHelper.AssignedBlocks, token);
            JObject blocksArray = await ApiHelper.GetRequestJTokenAsync(response);

            if (!blocksArray.HasValues)
                return;

            foreach (var block in blocksArray.Values())
            {
                if (!block.HasValues)
                    continue;

                JToken innerBlock = block[0];
                JToken serviceAreaId = innerBlock["serviceAreaId"];
                JToken offerPrice = innerBlock["bookedPrice"]["amount"];
                JToken startTime = innerBlock["startTime"];

                // The time the offer will be available for pick up at the facility
                int pickUpTimespan = (int)startTime - GetTimestamp();


                if ((float)offerPrice < minimumPrice || !areas.Contains((string)serviceAreaId) || pickUpTimespan < pickUpTimeThreshold)
                {
                    await ApiHelper.DeleteOfferAsync((int)startTime);
                }

            }

        }

    }
}
