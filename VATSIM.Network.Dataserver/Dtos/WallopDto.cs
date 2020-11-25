using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class WallopDto : FsdDto
    {
        public string From { get; }
        public string Message { get; }
        public string Cid { get; set; }
        public string Realname { get; set; }

        public WallopDto(string destination, string source, int packetNumber, int hopCount, string from, string message)
            : base(destination, source, packetNumber, hopCount)
        {
            From = from;
            Message = message;
        }

        public static WallopDto Deserialize(string[] fields)
        {
            if (fields.Length < 8)
            {
                throw new FormatException("Failed to parse MC packet.");
            }

            try
            {
                return new WallopDto(fields[1], fields[2], Convert.ToInt32(fields[3].Substring(1)),
                    Convert.ToInt32(fields[4]), fields[6], string.Join(":", fields[7..^0]));
            }
            catch (Exception e)
            {
                throw new FormatException("Failed to parse MC packet.", e);
            }
        }
    }
}