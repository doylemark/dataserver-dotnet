namespace VATSIM.Network.Dataserver.Models.V3
{
    public class FlightPlan
    {
        public string FlightRules { get; set; }
        public string Aircraft { get; set; }

        public string Departure { get; set; }
        public string Arrival { get; set; }
        public string Alternate { get; set; }

        public string CruiseTas { get; set; }
        public string Altitude { get; set; }
        public string Deptime { get; set; }
        public string EnrouteTime { get; set; }
        public string FuelTime { get; set; }
        public string Remarks { get; set; }
        public string Route { get; set; }
    }
}