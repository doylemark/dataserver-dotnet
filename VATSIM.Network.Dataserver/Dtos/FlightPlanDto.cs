using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class FlightPlanDto : FsdDto
    {
        internal const string Packet = "PLAN";
        public string Callsign { get; }
        public string Revision { get; }
        public string Type { get; }
        public string Aircraft { get; }
        public string CruiseSpeed { get; }
        public string DepartureAirport { get; }
        public string EstimatedDepartureTime { get; }
        public string ActualDepartureTime { get; }
        public string Altitude { get; }
        public string DestinationAirport { get; }
        public string HoursEnroute { get; }
        public string MinutesEnroute { get; }
        public string HoursFuel { get; }
        public string MinutesFuel { get; }
        public string AlternateAirport { get; }
        public string Remarks { get; }
        public string Route { get; }
        public string Cid { get; }
        public int AssignedTransponder { get; }
        public int HardLocked { get; }
        public string ModifiedByCid { get; }


        public FlightPlanDto(string destination, string source, int packetNumber, int hopCount, string callsign,
            string revision, string type, string aircraft, string cruiseSpeed, string departureAirport,
            string estimatedDepartureTime, string actualDepartureTime, string altitude, string destinationAirport,
            string hoursEnroute, string minutesEnroute, string hoursFuel, string minutesFuel, string alternateAirport,
            string remarks, string route, string cid, string modifiedby, int assignedtranspoonder, int hardlock) : base(destination, source, packetNumber, hopCount)
        {
            Callsign = callsign;
            Revision = revision;
            Type = type;
            Aircraft = aircraft;
            CruiseSpeed = cruiseSpeed;
            DepartureAirport = departureAirport;
            EstimatedDepartureTime = estimatedDepartureTime;
            ActualDepartureTime = actualDepartureTime;
            Altitude = altitude;
            DestinationAirport = destinationAirport;
            HoursEnroute = hoursEnroute;
            MinutesEnroute = minutesEnroute;
            HoursFuel = hoursFuel;
            MinutesFuel = minutesFuel;
            AlternateAirport = alternateAirport;
            Remarks = remarks;
            Route = route;
            Cid = cid;
            ModifiedByCid = modifiedby;
            AssignedTransponder = assignedtranspoonder;
            HardLocked = hardlock;
        }

        public static FlightPlanDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length < 21) // Shouldn't this be < 22?
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                return new FlightPlanDto(fields[0], fields[1], Convert.ToInt32(fields[2][1..]),
                    Convert.ToInt32(fields[3]), fields[4], fields[5], fields[6], fields[7],
                    fields[8], fields[9], fields[10], fields[11], fields[12], fields[13], fields[14], fields[15],
                    fields[16], fields[17], fields[18], fields[19], fields[20], fields[21], fields[22], Convert.ToInt32(fields[23]), Convert.ToInt32(fields[24]));
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }
    }
}