using System.Text;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class PongDto : FsdDto
    {
        public string Data { get; }

        public PongDto(string destination, string source, int packetNumber, int hopCount, string data) : base(
            destination, source, packetNumber, hopCount)
        {
            Data = data;
        }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder("PONG");
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
            message.Append(Data);
            return message.ToString();
        }
    }
}