using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models.V1;
using VATSIM.Network.Dataserver.Models;

namespace VATSIM.Network.Dataserver.Resources
{
    public class JsonFileResourceV1
    {
        public JsonGeneralData General { get; }
        public List<FsdClient> Clients { get; }
        public List<FsdServer> Servers { get; }
        public List<FsdClient> Prefiles { get; }

        public JsonFileResourceV1(List<FsdClient> clients, List<FsdServer> servers, List<FsdClient> prefiles, JsonGeneralData generalData)
        {
            General = generalData;
            Clients = clients;
            Servers = servers;
            Prefiles = prefiles;
        }
    }
}