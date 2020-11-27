using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class PingDto : FsdDto
    {
        internal const string Packet = "PING";
        public string Data { get; }

        public PingDto(string destination, string source, int packetNumber, int hopCount, string data) : base(
            destination, source, packetNumber, hopCount)
        {
            Data = data;
        }

        public static PingDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length < 5)
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                return new PingDto(fields[0], fields[1], Convert.ToInt32(fields[2][1..]),
                    Convert.ToInt32(fields[3]), fields[4]);
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }
    }
}