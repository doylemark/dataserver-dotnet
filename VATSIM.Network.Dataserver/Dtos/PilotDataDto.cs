using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class PilotDataDto : FsdDto
    {
        internal const string Packet = "PD";
        public string IdentFlag { get; }
        public string Callsign { get; }
        public int Transponder { get; }
        public int Rating { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public int Altitude { get; }
        public int GroundSpeed { get; }
        public int Heading { get; }
        public int PressureDifference { get; }

        public PilotDataDto(string destination, string source, int packetNumber, int hopCount, string identFlag,
            string callsign, int transponder, int rating, double latitude, double longitude, int altitude,
            int groundSpeed, int heading, int pressureDifference) : base(destination, source, packetNumber, hopCount)
        {
            IdentFlag = identFlag;
            Callsign = callsign;
            Transponder = transponder;
            Rating = rating;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            GroundSpeed = groundSpeed;
            Heading = heading;
            PressureDifference = pressureDifference;
        }

        public static PilotDataDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length < 13)
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                double hdgDbl = ParsePbh(fields[12]);
                return new PilotDataDto(fields[0], fields[1], Convert.ToInt32(fields[2][1..]),
                    Convert.ToInt32(fields[3]), fields[4], fields[5], Convert.ToInt32(fields[6]),
                    Convert.ToInt32(fields[7]),
                    Convert.ToDouble(fields[8]), Convert.ToDouble(fields[9]), Convert.ToInt32(fields[10]),
                    Convert.ToInt32(fields[11]),
                    Convert.ToInt32(hdgDbl), Convert.ToInt32(fields[13]));
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }

        private static double ParsePbh(string pbhField)
        {
            uint pbh = uint.Parse(pbhField);
            uint hdg = (pbh >> 2) & 0x3FF;
            double hdgDbl = hdg / 1024.0 * 360.0;
            if (hdgDbl < 0.0)
            {
                hdgDbl += 360.0;
            }
            else if (hdgDbl >= 360.0)
            {
                hdgDbl -= 360.0;
            }

            return hdgDbl;
        }
    }
}