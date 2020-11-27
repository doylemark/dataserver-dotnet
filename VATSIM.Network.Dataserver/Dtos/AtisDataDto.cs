using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class AtisDataDto : FsdDto
    {
        public string From { get; }
        public string Type { get; }
        public string Data { get; }

        public AtisDataDto(string destination, string source, int packetNumber, int hopCount, string from, string type,
            string data) : base(destination, source, packetNumber, hopCount)
        {
            From = from;
            Type = type;
            Data = data;
        }

        public static AtisDataDto Deserialize(string[] fields)
        {
            if (fields.Length < 10)
            {
                throw new FormatException("Failed to parse MC packet.");
            }

            try
            {
                return new AtisDataDto(fields[1], fields[2], Convert.ToInt32(fields[3][1..]),
                    Convert.ToInt32(fields[4]), fields[6], fields[8], fields[9]);
            }
            catch (Exception e)
            {
                throw new FormatException("Failed to parse MC packet.", e);
            }
        }
    }
}