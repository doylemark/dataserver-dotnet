using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using VATSIM.Network.Dataserver.Models;

namespace VATSIM.Network.Dataserver.Services
{
    public class HttpService
    {
        readonly RestClient restClient = new RestClient("https://api.vatsim.net/api");

        public async Task<ApiUserData> GetUserData(string cid)
        {
            string uri = "ratings/" + cid + "/?format=json";
            var request = new RestRequest(uri, Method.GET, DataFormat.Json);
            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

            IRestResponse response = await restClient.ExecuteAsync(request);

            return !string.IsNullOrEmpty(response.Content) ? JsonConvert.DeserializeObject<ApiUserData>(response.Content) : new ApiUserData();
        }
    }
}
