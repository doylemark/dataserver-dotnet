using System;
using System.Text;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class NotifyDto : FsdDto
    {
        internal const string Packet = "NOTIFY";
        public int FeedFlag { get; }
        public string Ident { get; }
        public string Name { get; }
        public string Email { get; }
        public string Hostname { get; }
        public string Version { get; }
        public int Flags { get; }
        public string Location { get; }

        public NotifyDto(string destination, string source, int packetNumber, int hopCount, int feedFlag, string ident,
            string name, string email, string hostname, string version, int flags, string location) : base(destination,
            source, packetNumber, hopCount)
        {
            FeedFlag = feedFlag;
            Ident = ident;
            Name = name;
            Email = email;
            Hostname = hostname;
            Version = version;
            Flags = flags;
            Location = location;
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
            message.Append(FeedFlag);
            message.Append(":");
            message.Append(Ident);
            message.Append(":");
            message.Append(Name);
            message.Append(":");
            message.Append(Email);
            message.Append(":");
            message.Append(Hostname);
            message.Append(":");
            message.Append(Version);
            message.Append(":");
            message.Append(Flags);
            message.Append(":");
            message.Append(Location);
            return message.ToString();
        }

        public static NotifyDto Deserialize(string payload)
        {
            string[] fields = payload.Split(":");
            if (fields.Length < 12)
            {
                throw new FormatException($"Failed to parse {Packet} packet.");
            }

            try
            {
                return new NotifyDto(fields[0], fields[1], Convert.ToInt32(fields[2][1..]),
                    Convert.ToInt32(fields[3]), Convert.ToInt32(fields[4]), fields[5], fields[6], fields[7],
                    fields[8], fields[9], Convert.ToInt32(fields[10]), fields[11]);
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to parse {Packet} packet.", e);
            }
        }
    }
}