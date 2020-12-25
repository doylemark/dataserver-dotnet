using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models.V1;

namespace VATSIM.Network.Dataserver.Models.Resources
{
    public class JsonFileResourceV2
    {
        public JsonGeneralData General { get; set; }
        public List<FsdClient> Pilots { get; set; }
        public List<FsdClient> Controllers { get; set; }
        public List<FsdClient> Atis { get; set; }
        public List<FsdServer> Servers { get; set; }
        public List<FsdClient> Prefiles { get; set; }

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