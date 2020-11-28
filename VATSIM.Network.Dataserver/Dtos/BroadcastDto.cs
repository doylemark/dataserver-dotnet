using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class BroadcastDto : FsdDto
    {
        public string From { get; }
        public string Message { get; }
        public string Cid { get; set; }
        public string Realname { get; set; }

        public BroadcastDto(string destination, string source, int packetNumber, int hopCount, string from, string message)
            : base(destination, source, packetNumber, hopCount)
        {
            From = from;
            Message = message;
        }

        public static BroadcastDto Deserialize(string[] fields)
        {
            if (fields.Length < 8)
            {
                throw new FormatException("Failed to parse MC packet.");
            }

            try
            {
                return new BroadcastDto(fields[1], fields[2], Convert.ToInt32(fields[3][1..]),
                    Convert.ToInt32(fields[4]), fields[6], string.Join(":", fields[7..^0]));
            }
            catch (Exception e)
            {
                throw new FormatException("Failed to parse MC packet.", e);
            }
        }
    }
}