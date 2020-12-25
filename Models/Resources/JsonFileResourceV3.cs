using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models.V3;

namespace VATSIM.Network.Dataserver.Models.Resources
{
    public class JsonFileResourceV3
    {
        public JsonGeneralData General { get; set; }
        public List<FsdPilot> Pilots { get; set; }
        public List<FsdController> Controllers { get; set; }
        public List<FsdAtis> Atis { get; set; }
        public List<FsdServer> Servers { get; set; }
        public List<FsdPrefile> Prefiles { get; set; }
        public List<AtcFacility> Facilities { get; set; }
        public List<Rating> Ratings { get; set; }
        public List<PilotRating> PilotRatings { get; set; }

        public JsonFileResourceV3(List<FsdPilot> pilots, List<FsdController> controllers, List<FsdAtis> atis, List<FsdServer> servers, List<FsdPrefile> prefiles, List<AtcFacility> facilities, List<Rating> ratings, List<PilotRating> pilotRatings, JsonGeneralData generalData)
        {
            General = generalData;
            Pilots = pilots;
            Controllers = controllers;
            Atis = atis;
            Servers = servers;
            Prefiles = prefiles;
            Facilities = facilities;
            Ratings = ratings;
            PilotRatings = pilotRatings;
        }
    }
}