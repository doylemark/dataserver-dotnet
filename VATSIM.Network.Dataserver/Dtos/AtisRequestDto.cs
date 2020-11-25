using System.Text;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class AtisRequestDto : FsdDto
    {
        public string From { get; }

        public AtisRequestDto(string destination, string source, int packetNumber, int hopCount, string from) : base(
            destination, source, packetNumber, hopCount)
        {
            From = from;
        }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder("MC");
            message.Append(":");
            message.Append("%");
            message.Append(Destination);
            message.Append(":");
            message.Append(Source);
            message.Append(":");
            message.Append("U");
            message.Append(PacketNumber);
            message.Append(":");
            message.Append(HopCount);
            message.Append(":");
            message.Append("24");
            message.Append(":");
            message.Append(From);
            message.Append(":");
            message.Append("ATIS");
            return message.ToString();
        }
    }
}