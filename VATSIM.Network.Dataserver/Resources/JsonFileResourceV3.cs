using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models;
using VATSIM.Network.Dataserver.Models.V3;

namespace VATSIM.Network.Dataserver.Resources
{
    public class JsonFileResourceV3
    {
        public JsonGeneralData General { get; }
        public List<FsdPilot> Pilots { get; }
        public List<FsdController> Controllers { get; }
        public List<FsdAtis> Atis { get; }
        public List<FsdServer> Servers { get; }
        public List<FsdPrefile> Prefiles { get; }
        public List<AtcFacility> Facilities { get; }
        public List<Rating> Ratings { get; }
        public List<PilotRating> PilotRatings { get; }

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