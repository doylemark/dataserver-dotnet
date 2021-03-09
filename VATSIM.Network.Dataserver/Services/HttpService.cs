using System;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using VATSIM.Network.Dataserver.Models;
using VATSIM.Network.Dataserver.HttpModels;

namespace VATSIM.Network.Dataserver.Services
{
    public class HttpService
    {
        private readonly RestClient _restClient = new RestClient(Environment.GetEnvironmentVariable("API_URL") ?? string.Empty);

        public async Task<ApiUserData> GetUserData(string cid)
        {
            string uri = "ratings/" + cid + "/?format=json";
            RestRequest request = new RestRequest(uri, Method.GET, DataFormat.Json)
            {
                OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; }
            };

            IRestResponse response = await _restClient.ExecuteAsync(request);

            return !string.IsNullOrEmpty(response.Content) ? JsonConvert.DeserializeObject<ApiUserData>(response.Content) : new ApiUserData();
        }
    }
}
 