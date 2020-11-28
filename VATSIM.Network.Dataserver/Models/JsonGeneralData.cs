using System;

namespace VATSIM.Network.Dataserver.Models
{
    public class JsonGeneralData
    {
        public int Version { get; set; }
        public int Reload { get; set; }
        public string Update { get; set; }
        public DateTime UpdateTimestamp { get; set; }
        public int ConnectedClients { get; set; }
        public int UniqueUsers { get; set; }
    }
}