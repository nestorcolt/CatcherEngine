using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;

namespace SearchEngine.Models
{
    public interface IBlockCatcher
    {
        Task DeactivateUser(string userId);
        bool ScheduleHasData(JToken searchSchedule);
        bool ValidateArea(string serviceAreaId, List<string> areas);
        int GetTimestamp();
        Dictionary<string, string> EmulateDevice(Dictionary<string, string> requestDictionary);
        Task AcceptSingleOfferAsync(JToken block, UserDto userDto);
        void AcceptOffers(JToken offerList, UserDto userDto);
        Task<HttpStatusCode> GetOffersAsyncHandle(UserDto userDto, Dictionary<string, string> requestHeaders, string serviceAreaId);
        Task<bool> LookingForBlocks(UserDto userDto);
    }
}