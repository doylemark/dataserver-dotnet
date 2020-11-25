using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models;
using VATSIM.Network.Dataserver.Models.V1;

namespace VATSIM.Network.Dataserver.Resources
{
    public class JsonFileResourceV2
    {
        public JsonGeneralData General { get; }
        public List<FsdClient> Pilots { get; }
        public List<FsdClient> Controllers { get; }
        public List<FsdClient> Atis { get; }
        public List<FsdServer> Servers { get; }
        public List<FsdClient> Prefiles { get; }

        public JsonFileResourceV2(List<FsdClient> pilots, List<FsdClient> controllers, List<FsdClient> atis, List<FsdServer> servers, List<FsdClient> prefiles, JsonGeneralData generalData)
        {
            General = generalData;
            Pilots = pilots;
            Controllers = controllers;
            Atis = atis;
            Servers = servers;
            Prefiles = prefiles;
        }
    }
}