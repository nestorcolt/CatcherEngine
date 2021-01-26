using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace SearchEngine.Modules
{
    [DataContract]
    public class UserDto
    {
        [DataMember(Name = "user_id")]
        public string UserId { get; set; }

        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "refresh_token")]
        public string RefreshToken { get; set; }

        [DataMember(Name = "last_active")]
        public long LastActive { get; set; }

        [DataMember(Name = "minimum_price")]
        public long MinimumPrice { get; set; }

        [DataMember(Name = "search_schedule")]
        public JToken SearchSchedule { get; set; }

        [DataMember(Name = "areas")]
        public List<string> Areas { get; set; }
    }

}
