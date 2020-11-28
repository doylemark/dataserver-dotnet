using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class RemoveClientDto : FsdDto
    {
        internal const string Packet = "RMCLIENT";
        public string Callsign { get; }

        public RemoveClientDto(string destination, string source, int packetNumber, int hopCount, string callsign) :
            base(destination, source, packetNumber, hopCount)
        {
            Callsign = callsign;
        }

        public static RemoveClientDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length < 5)
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                return new RemoveClientDto(fields[0], fields[1], Convert.ToInt32(fields[2][1..]),
                    Convert.ToInt32(fields[3]), fields[4]);
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }
    }
}