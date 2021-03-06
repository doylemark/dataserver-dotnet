using System;
using Newtonsoft.Json;

namespace VATSIM.Network.Dataserver.Models.V1
{
    public class FsdClient
    {
        public string Callsign { get; set; }
        public string Cid { get; set; }
        public string Realname { get; set; }
        public string Clienttype { get; set; }
        public string Frequency { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Altitude { get; set; }
        public int Groundspeed { get; set; }
        public string PlannedAircraft { get; set; }
        public string PlannedTascruise { get; set; }
        public string PlannedDepairport { get; set; }
        public string PlannedAltitude { get; set; }
        public string PlannedDestairport { get; set; }
        public string Server { get; set; }
        public int Protrevision { get; set; }
        public int Rating { get; set; }
        public int Transponder { get; set; }
        public int Facilitytype { get; set; }
        public int Visualrange { get; set; }
        public string PlannedRevision { get; set; }
        public string PlannedFlighttype { get; set; }
        public string PlannedDeptime { get; set; }
        public string PlannedActdeptime { get; set; }
        public string PlannedHrsenroute { get; set; }
        public string PlannedMinenroute { get; set; }
        public string PlannedHrsfuel { get; set; }
        public string PlannedMinfuel { get; set; }
        public string PlannedAltairport { get; set; }
        public string PlannedRemarks { get; set; }
        public string PlannedRoute { get; set; }
        public double PlannedDepairportLat { get; set; }
        public double PlannedDepairportLon { get; set; }
        public double PlannedDestairportLat { get; set; }
        public double PlannedDestairportLon { get; set; }
        public string AtisMessage { get; set; }
        public DateTime TimeLastAtisReceived { get; set; }
        public DateTime TimeLogon { get; set; }
        public int Heading { get; set; }
        public double QnhIHg { get; set; }
        public int QnhMb { get; set; }
        [JsonIgnore] public bool AppendAtis { get; set; }
        [JsonIgnore] public DateTime LastUpdated { get; set; }
    }
}