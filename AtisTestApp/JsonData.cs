using System;
using Newtonsoft.Json;

namespace AtisTestApp
{
    public class JsonData
    {
        [JsonProperty("general")]
        public General General { get; set; }

        [JsonProperty("pilots")]
        public Pilot[] Pilots { get; set; }

        [JsonProperty("controllers")]
        public Ati[] Controllers { get; set; }

        [JsonProperty("atis")]
        public Ati[] Atis { get; set; }

        [JsonProperty("servers")]
        public ServerElement[] Servers { get; set; }

        [JsonProperty("prefiles")]
        public Prefile[] Prefiles { get; set; }

        [JsonProperty("facilities")]
        public Facility[] Facilities { get; set; }

        [JsonProperty("ratings")]
        public Facility[] Ratings { get; set; }

        [JsonProperty("pilot_ratings")]
        public PilotRating[] PilotRatings { get; set; }
    }

    public class Ati
    {
        [JsonProperty("cid")]
        public long Cid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("callsign")]
        public string Callsign { get; set; }

        [JsonProperty("frequency")]
        public string Frequency { get; set; }

        [JsonProperty("facility")]
        public long Facility { get; set; }

        [JsonProperty("rating")]
        public long Rating { get; set; }

        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("visual_range")]
        public long VisualRange { get; set; }

        [JsonProperty("text_atis")]
        public string[] TextAtis { get; set; }

        [JsonProperty("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty("logon_time")]
        public DateTimeOffset LogonTime { get; set; }
    }

    public class Facility
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("short")]
        public string Short { get; set; }

        [JsonProperty("long")]
        public string Long { get; set; }
    }

    public class General
    {
        [JsonProperty("version")]
        public long Version { get; set; }

        [JsonProperty("reload")]
        public long Reload { get; set; }

        [JsonProperty("update")]
        public string Update { get; set; }

        [JsonProperty("update_timestamp")]
        public DateTimeOffset UpdateTimestamp { get; set; }

        [JsonProperty("connected_clients")]
        public long ConnectedClients { get; set; }

        [JsonProperty("unique_users")]
        public long UniqueUsers { get; set; }
    }

    public class PilotRating
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("long_name")]
        public string LongName { get; set; }
    }

    public class Pilot
    {
        [JsonProperty("cid")]
        public long Cid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("callsign")]
        public string Callsign { get; set; }

        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("pilot_rating")]
        public long PilotRating { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("altitude")]
        public long Altitude { get; set; }

        [JsonProperty("groundspeed")]
        public long Groundspeed { get; set; }

        [JsonProperty("transponder")]
        public long Transponder { get; set; }

        [JsonProperty("heading")]
        public long Heading { get; set; }

        [JsonProperty("qnh_i_hg")]
        public double QnhIHg { get; set; }

        [JsonProperty("qnh_mb")]
        public long QnhMb { get; set; }

        [JsonProperty("flight_plan")]
        public FlightPlan FlightPlan { get; set; }

        [JsonProperty("logon_time")]
        public DateTimeOffset LogonTime { get; set; }

        [JsonProperty("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }
    }

    public class FlightPlan
    {
        [JsonProperty("flight_rules")]
        public string FlightRules { get; set; }

        [JsonProperty("aircraft")]
        public string Aircraft { get; set; }

        [JsonProperty("departure")]
        public string Departure { get; set; }

        [JsonProperty("arrival")]
        public string Arrival { get; set; }

        [JsonProperty("alternate")]
        public string Alternate { get; set; }

        [JsonProperty("cruise_tas")]
        public string CruiseTas { get; set; }

        [JsonProperty("altitude")]
        public string Altitude { get; set; }

        [JsonProperty("deptime")]
        public string Deptime { get; set; }

        [JsonProperty("enroute_time")]
        public string EnrouteTime { get; set; }

        [JsonProperty("fuel_time")]
        public string FuelTime { get; set; }

        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [JsonProperty("route")]
        public string Route { get; set; }
    }

    public class Prefile
    {
        [JsonProperty("cid")]
        public long Cid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("callsign")]
        public string Callsign { get; set; }

        [JsonProperty("flight_plan")]
        public FlightPlan FlightPlan { get; set; }

        [JsonProperty("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }
    }

    public class ServerElement
    {
        [JsonProperty("ident")]
        public string Ident { get; set; }

        [JsonProperty("hostname_or_ip")]
        public string HostnameOrIp { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("clients_connection_allowed")]
        public long ClientsConnectionAllowed { get; set; }
    }
}
