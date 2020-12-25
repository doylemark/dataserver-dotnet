using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models.V1;

namespace VATSIM.Network.Dataserver.Models.Resources
{
    public class JsonFileResourceV1
    {
        public JsonGeneralData General { get; set; }
        public List<FsdClient> Clients { get; set; }
        public List<FsdServer> Servers { get; set; }
        public List<FsdClient> Prefiles { get; set; }

        public JsonFileResourceV1(List<FsdClient> clients, List<FsdServer> servers, List<FsdClient> prefiles, JsonGeneralData generalData)
        {
            General = generalData;
            Clients = clients;
            Servers = servers;
            Prefiles = prefiles;
        }
    }
}