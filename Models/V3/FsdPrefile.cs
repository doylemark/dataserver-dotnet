using System;

namespace VATSIM.Network.Dataserver.Models.V3
{
    public class FsdPrefile
    {
        public int Cid { get; set; }
        public string Name { get; set; }
        public string Callsign { get; set; }
        public FlightPlan FlightPlan { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}