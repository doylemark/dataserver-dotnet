using System;
using System.Text;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class AddClientDto : FsdDto
    {
        internal const string Packet = "ADDCLIENT";
        public string Callsign { get; }
        public string Cid { get; }
        public int Hidden { get; }
        public int ProtocolRevision { get; }
        public int Rating { get; }
        public string RealName { get; }
        public string Server { get; }
        public int SimType { get; }
        public int Type { get; }

        public AddClientDto(string destination, string source, int packetNumber, int hopCount, string cid,
            string server, string callsign, int type, int rating, int protocolRevision, string realName, int simType,
            int hidden) : base(destination, source, packetNumber, hopCount)
        {
            Cid = cid;
            Server = server;
            Callsign = callsign;
            Type = type;
            Rating = rating;
            ProtocolRevision = protocolRevision;
            RealName = realName;
            SimType = simType;
            Hidden = hidden;
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
            message.Append(Cid);
            message.Append(":");
            message.Append(Server);
            message.Append(":");
            message.Append(Callsign);
            message.Append(":");
            message.Append(Type);
            message.Append(":");
            message.Append(Rating);
            message.Append(":");
            message.Append(ProtocolRevision);
            message.Append(":");
            message.Append(RealName);
            message.Append(":");
            message.Append(SimType);
            message.Append(":");
            message.Append(Hidden);
            return message.ToString();
        }

        public static AddClientDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length < 11)
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                return new AddClientDto(fields[0], fields[1], Convert.ToInt32(fields[2][1..]),
                    Convert.ToInt32(fields[3]), fields[4], fields[5], fields[6], Convert.ToInt32(fields[7]),
                    Convert.ToInt32(fields[8]), Convert.ToInt32(fields[9]), fields[10], Convert.ToInt32(fields[11]),
                    Convert.ToInt32(fields[12]));
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }
    }
}