using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace VATSIM.Network.Dataserver.Models.V3
{
    public class FsdAtis
    {
        public int Cid { get; set; }
        public string Name { get; set; }
        public string Callsign { get; set; }
        public string Frequency { get; set; }
        public int Facility { get; set; }
        public int Rating { get; set; }
        public string Server { get; set; }
        public int VisualRange { get; set; }
        public string AtisCode { get; set; }
        public List<string> TextAtis { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LogonTime { get; set; }
        [JsonIgnore] public bool AppendAtis { get; set; }
    }
}