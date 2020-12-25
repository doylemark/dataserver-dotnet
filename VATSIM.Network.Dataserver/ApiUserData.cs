using Newtonsoft.Json;

namespace VATSIM.Network.Dataserver.Models
{
    public class ApiUserData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("rating")]
        public int Rating { get; set; }

        [JsonProperty("pilotrating")]
        public int PilotRating { get; set; }

        [JsonProperty("name_first")]
        public string FirstName { get; set; }

        [JsonProperty("name_last")]
        public string LastName { get; set; }

        [JsonProperty("age")]
        public int Age { get; set; }

        [JsonProperty("countystate")]
        public string CountyState { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("susp_date", NullValueHandling = NullValueHandling.Ignore)]
        public string SuspDate { get; set; }

        [JsonProperty("reg_date")]
        public string RegDate { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("division")]
        public string Division { get; set; }

        [JsonProperty("subdivision")]
        public string SubDivision { get; set; }
    }
}
