using Newtonsoft.Json;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class FsdDto
    {
        [JsonIgnore] public string Destination { get; }
        [JsonIgnore] public string Source { get; }
        [JsonIgnore] public int PacketNumber { get; }
        [JsonIgnore] public int HopCount { get; }

        public FsdDto(string destination, string source, int packetNumber, int hopCount)
        {
            Destination = destination;
            Source = source;
            PacketNumber = packetNumber;
            HopCount = hopCount;
        }
    }
}