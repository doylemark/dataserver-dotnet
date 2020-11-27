using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class FlightPlanCancelDto : FsdDto
    {
        internal const string Packet = "DELPLAN";
        public string Callsign { get; }

        public FlightPlanCancelDto(string destination, string source, int packetNumber, int hopCount, string callsign) : base(destination, source, packetNumber, hopCount)
        {
            Callsign = callsign;
        }

        public static FlightPlanCancelDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length != 5)
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                return new FlightPlanCancelDto(fields[0], fields[1], Convert.ToInt32(fields[2][1..]),
                    Convert.ToInt32(fields[3]), fields[4]);
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }
    }
}