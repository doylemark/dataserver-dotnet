namespace VATSIM.Network.Dataserver.Models.V1
{
    public class FsdServer
    {
        public string Ident { get; set; }
        public string HostnameOrIp { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public int ClientsConnectionAllowed { get; set; }
    }
}