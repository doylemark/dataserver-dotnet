using System;
using Newtonsoft.Json;

namespace VATSIM.Network.Dataserver.Models.V3
{
    public class FsdPilot
    {
        public int Cid { get; set; }
        public string Name { get; set; }
        public string Callsign { get; set; }
        public string Server { get; set; }
        public int PilotRating { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Altitude { get; set; }
        public int Groundspeed { get; set; }
        public string Transponder { get; set; }
        public int Heading { get; set; }
        public double QnhIHg { get; set; }
        public int QnhMb { get; set; }
        public FlightPlan FlightPlan { get; set; }
        public DateTime LogonTime { get; set; }
        public DateTime LastUpdated { get; set; }
        [JsonIgnore] public bool PilotRatingSet { get; set; }
        [JsonIgnore] public bool HasPilotData { get; set; }
    }
}