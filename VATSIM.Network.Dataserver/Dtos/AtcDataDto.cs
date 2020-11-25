using System;
using System.Text;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class AtcDataDto : FsdDto
    {
        internal const string Packet = "AD";
        public string Callsign { get; }
        public string Frequency { get; }
        public int FacilityType { get; }
        public int VisualRange { get; }
        public int Rating { get; }
        public double Latitude { get; }
        public double Longitude { get; }

        public AtcDataDto(string destination, string source, int packetNumber, int hopCount, string callsign,
            string frequency, int facilityType, int visualRange, int rating, double latitude, double longitude) : base(
            destination, source, packetNumber, hopCount)
        {
            Callsign = callsign;
            Frequency = frequency;
            FacilityType = facilityType;
            VisualRange = visualRange;
            Rating = rating;
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder(Packet);
            message.Append(":");
            message.Append(Destination);
            message.Append(":");
            message.Append(Source);
            message.Append(":");
            message.Append("B");
            message.Append(PacketNumber);
            message.Append(":");
            message.Append(HopCount);
            message.Append(":");
            message.Append(Callsign);
            message.Append(":");
            message.Append(Frequency);
            message.Append(":");
            message.Append(FacilityType);
            message.Append(":");
            message.Append(VisualRange);
            message.Append(":");
            message.Append(Rating);
            message.Append(":");
            message.Append(Latitude);
            message.Append(":");
            message.Append(Longitude);
            message.Append(":");
            message.Append("0"); // Transceiver altitude
            return message.ToString();
        }

        public static AtcDataDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length < 12)
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                return new AtcDataDto(fields[0], fields[1], Convert.ToInt32(fields[2].Substring(1)),
                    Convert.ToInt32(fields[3]), fields[4], fields[5], Convert.ToInt32(fields[6]),
                    Convert.ToInt32(fields[7]),
                    Convert.ToInt32(fields[8]), Convert.ToDouble(fields[9]), Convert.ToDouble(fields[10]));
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }
    }
}