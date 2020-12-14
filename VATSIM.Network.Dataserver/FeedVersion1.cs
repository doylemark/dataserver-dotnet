using System;
using System.Collections.Generic;
using System.Text;
using Timer = System.Timers.Timer;
using VATSIM.Network.Dataserver.Models.V1;
using VATSIM.Network.Dataserver.Services;
using VATSIM.Network.Dataserver.Dtos;
using System.Timers;
using System.Linq;
using VATSIM.Network.Dataserver.Models;
using Prometheus;
using Amazon.S3;
using VATSIM.Network.Dataserver.Resources;
using Newtonsoft.Json.Serialization;
using Amazon.S3.Model;
using Newtonsoft.Json;

namespace VATSIM.Network.Dataserver
{
    public class FeedVersion1
    {
        private readonly HttpService _httpService = new HttpService();

        private readonly FsdConsumer _fsdConsumer = new FsdConsumer(Environment.GetEnvironmentVariable("FSD_HOST"), int.Parse(Environment.GetEnvironmentVariable("FSD_PORT") ?? "4113"));
        private const string ConsumerName = "DSERVER";
        private const string ConsumerCallsign = "DCLIENT";

        private readonly LiveFeedProducer _liveFeedProducer = new LiveFeedProducer(Environment.GetEnvironmentVariable("KAFKA_SERVER"), Environment.GetEnvironmentVariable("KAFKA_USERNAME"), Environment.GetEnvironmentVariable("KAFKA_PASSWORD"));

        private readonly List<FsdClient> _fsdClients = new List<FsdClient>();
        private readonly List<FsdClient> _fsdPrefiles = new List<FsdClient>();
        private readonly List<FsdServer> _fsdServers = new List<FsdServer>();
        private readonly Timer _timeoutTimer = new Timer(60000);
        private readonly Timer _prometheusMetricsTimer = new Timer(5000);

        private readonly Timer _fileTimer = new Timer(15000);
        private static readonly AmazonS3Client AmazonS3Client = new AmazonS3Client(new AmazonS3Config
        {
            ServiceURL = Environment.GetEnvironmentVariable("S3_URL")
        });

        private readonly Gauge _totalConnections = Prometheus.Metrics.CreateGauge("fsd_total_connections",
            "Total number of connections to the FSD network.", new GaugeConfiguration
            {
                LabelNames = new[] { "server" },
                SuppressInitialValue = true
            });

        private readonly Gauge _uniqueConnections = Prometheus.Metrics.CreateGauge("fsd_unique_connections",
            "Unique number of connections to the FSD network.", new GaugeConfiguration
            {
                SuppressInitialValue = true
            });

        private readonly Gauge _totalAtisConnections = Prometheus.Metrics.CreateGauge("fsd_atis_connections",
            "Number of ATIS connections.", new GaugeConfiguration
            {
                LabelNames = new[] { "is_empty", "server" },
                SuppressInitialValue = true
            });

        public readonly Counter SpacesWrites = Prometheus.Metrics.CreateCounter("fsd_spaces_writes",
            "Number of writes to spaces.", new CounterConfiguration
            {
                LabelNames = new[] { "failed" }
            });

        public void StartFeedVersion1()
        {
            _fsdConsumer.AddClientDtoReceived += FsdConsumer_AddClientDtoReceived;
            _fsdConsumer.RemoveClientDtoReceived += FsdConsumer_RemoveClientDtoReceived;
            _fsdConsumer.PilotDataDtoReceived += FsdConsumer_PilotDataDtoReceived;
            _fsdConsumer.AtcDataDtoReceived += FsdConsumer_AtcDataDtoReceived;
            _fsdConsumer.FlightPlanDtoReceived += FsdConsumer_FlightPlanDtoReceived;
            _fsdConsumer.FlightPlanCancelDtoReceived += FsdConsumer_FlightPlanCancelDtoReceived;
            _fsdConsumer.AtisDataDtoReceived += FsdConsumer_AtisDataDtoReceived;
            _fsdConsumer.NotifyDtoReceived += FsdConsumer_NotifyDtoReceived;
            _fsdConsumer.WallopDtoReceived += FsdConsumer_WallopDtoReceived;
            _fsdConsumer.BroadcastDtoReceived += FsdConsumer_BroadcastDtoReceived;
            _fsdConsumer.AtisTimer.Elapsed += FsdConsumer_AtisTimerElapsed;
            _timeoutTimer.Elapsed += RemoveTimedOutConnections;
            _prometheusMetricsTimer.Elapsed += SetPrometheusConnectionCounts;

            _fsdConsumer.Start(ConsumerName, ConsumerCallsign);
            _timeoutTimer.Start();
            _fsdConsumer.AtisTimer.Start();

            _fileTimer.Elapsed += WriteDataFiles;
            _fileTimer.Start();

            _prometheusMetricsTimer.Start();
            MetricServer metricServer = new MetricServer(port: 8501);
            metricServer.Start();

            Console.WriteLine("Starting Feed Version 1 & 2");
        }

        private async void FsdConsumer_AddClientDtoReceived(object sender, DtoReceivedEventArgs<AddClientDto> p)
        {
            if (_fsdClients.Any(c => c.Callsign == p.Dto.Callsign) || p.Dto.Callsign == "AFVDATA" || p.Dto.Callsign == "SUP" || p.Dto.Callsign == "DATA" || p.Dto.Callsign == "DATASVR" || p.Dto.Callsign.Contains("DCLIENT") || p.Dto.Callsign == "DATA-TOR")
            {
                return;
            }

            FsdClient fsdClient = new FsdClient
            {
                Callsign = p.Dto.Callsign,
                Cid = p.Dto.Cid,
                Protrevision = p.Dto.ProtocolRevision,
                Rating = p.Dto.Rating,
                Realname = p.Dto.RealName,
                Server = p.Dto.Server,
                Clienttype = p.Dto.Type == 1 ? "PILOT" : "ATC",
                TimeLogon = DateTime.UtcNow,
                TimeLastAtisReceived = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            _fsdClients.Add(fsdClient);
            _fsdPrefiles.RemoveAll(f => f.Callsign == p.Dto.Callsign);
            await _liveFeedProducer.ProduceMessage(p.Dto);
            Console.WriteLine($"{p.Dto.Callsign} connected to the network - {_fsdClients.Count}");
        }

        private async void FsdConsumer_RemoveClientDtoReceived(object sender, DtoReceivedEventArgs<RemoveClientDto> p)
        {
            _fsdClients.RemoveAll(c => c.Callsign == p.Dto.Callsign);
            await _liveFeedProducer.ProduceMessage(p.Dto);
            Console.WriteLine($"{p.Dto.Callsign} disconnected from the network - {_fsdClients.Count}");
        }

        private async void FsdConsumer_PilotDataDtoReceived(object sender, DtoReceivedEventArgs<PilotDataDto> p)
        {
            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.Callsign);
            if (fsdClient == null) return;
            fsdClient.Transponder = p.Dto.Transponder;
            fsdClient.Latitude = p.Dto.Latitude;
            fsdClient.Longitude = p.Dto.Longitude;
            fsdClient.Altitude = p.Dto.Altitude;
            fsdClient.Groundspeed = p.Dto.GroundSpeed;
            fsdClient.Heading = p.Dto.Heading;
            fsdClient.QnhIHg = Math.Round(29.92 - (p.Dto.PressureDifference / 1000.0), 2);
            fsdClient.QnhMb = (int)Math.Round(fsdClient.QnhIHg * 33.864);
            fsdClient.LastUpdated = DateTime.UtcNow;
            await _liveFeedProducer.ProduceMessage(p.Dto);
        }

        private async void FsdConsumer_AtcDataDtoReceived(object sender, DtoReceivedEventArgs<AtcDataDto> p)
        {
            if (p.Dto.Callsign == "AFVDATA" || p.Dto.Callsign == "SUP" || p.Dto.Callsign == "DATA" || p.Dto.Callsign == "DATASVR" || p.Dto.Callsign.Contains("DCLIENT") || p.Dto.Callsign == "DATA-TOR")
            {
                return;
            }

            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.Callsign);
            if (fsdClient == null) return;
            fsdClient.Frequency = p.Dto.Frequency.Insert(2, ".").Insert(0, "1");
            fsdClient.Latitude = p.Dto.Latitude;
            fsdClient.Longitude = p.Dto.Longitude;
            fsdClient.Facilitytype = p.Dto.FacilityType;
            fsdClient.Visualrange = p.Dto.VisualRange;
            fsdClient.LastUpdated = DateTime.UtcNow;
            await _liveFeedProducer.ProduceMessage(p.Dto);
        }

        private async void FsdConsumer_FlightPlanDtoReceived(object sender, DtoReceivedEventArgs<FlightPlanDto> p)
        {
            try
            {
                FsdClient fsdClient;
                bool prefile;
                if (_fsdClients.All(c => c.Callsign != p.Dto.Callsign))
                {
                    ApiUserData response = await _httpService.GetUserData(p.Dto.Cid);
                    Console.WriteLine($"Prefile Received for {response.FirstName} {response.LastName}");
                    fsdClient = new FsdClient
                    {
                        Cid = p.Dto.Cid,
                        Realname = $"{response.FirstName} {response.LastName}",
                        Callsign = p.Dto.Callsign,
                        LastUpdated = DateTime.UtcNow,
                    };
                    prefile = true;
                }
                else
                {
                    fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.Callsign);
                    prefile = false;
                }

                if (fsdClient == null) return;
                fsdClient.PlannedAircraft = p.Dto.Aircraft;
                fsdClient.PlannedTascruise = p.Dto.CruiseSpeed;
                fsdClient.PlannedDepairport = p.Dto.DepartureAirport;
                fsdClient.PlannedAltitude = p.Dto.Altitude;
                fsdClient.PlannedDestairport = p.Dto.DestinationAirport;
                fsdClient.PlannedRevision = p.Dto.Revision;
                fsdClient.PlannedFlighttype = p.Dto.Type;
                fsdClient.PlannedDeptime = p.Dto.EstimatedDepartureTime;
                fsdClient.PlannedActdeptime = p.Dto.ActualDepartureTime;
                fsdClient.PlannedHrsenroute = p.Dto.HoursEnroute;
                fsdClient.PlannedMinenroute = p.Dto.MinutesEnroute;
                fsdClient.PlannedHrsfuel = p.Dto.HoursFuel;
                fsdClient.PlannedMinfuel = p.Dto.MinutesFuel;
                fsdClient.PlannedAltairport = p.Dto.AlternateAirport;
                fsdClient.PlannedRemarks = p.Dto.Remarks;
                fsdClient.PlannedRoute = p.Dto.Route;
                if (prefile)
                {
                    _fsdPrefiles.Add(fsdClient);
                    p.Dto.Prefile = true;
                }

                p.Dto.Realname = fsdClient.Realname;
                await _liveFeedProducer.ProduceMessage(p.Dto);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async void FsdConsumer_FlightPlanCancelDtoReceived(object sender, DtoReceivedEventArgs<FlightPlanCancelDto> p)
        {
            _fsdPrefiles.RemoveAll(c => c.Callsign == p.Dto.Callsign);
            await _liveFeedProducer.ProduceMessage(p.Dto);
        }

        private async void FsdConsumer_AtisDataDtoReceived(object sender, DtoReceivedEventArgs<AtisDataDto> p)
        {
            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.From);
            if (fsdClient == null) return;
            switch (p.Dto.Type)
            {
                case "T" when fsdClient.AppendAtis:
                    fsdClient.AtisMessage += $"^§{p.Dto.Data}";
                    break;
                case "T":
                    fsdClient.AtisMessage = p.Dto.Data;
                    fsdClient.AppendAtis = true;
                    break;
                case "E":
                    fsdClient.AppendAtis = false;
                    break;
            }

            await _liveFeedProducer.ProduceMessage(p.Dto);
        }

        private async void FsdConsumer_NotifyDtoReceived(object sender, DtoReceivedEventArgs<NotifyDto> p)
        {
            if (p.Dto.Hostname == "127.0.0.1" || p.Dto.Name.ToLower().Contains("data") || p.Dto.Name.ToLower().Contains("testing") || p.Dto.Name.ToLower().Contains("afv") || _fsdServers.Any(s => s.Name == p.Dto.Name))
            {
                return;
            }

            FsdServer fsdServer = new FsdServer
            {
                Ident = p.Dto.Ident,
                HostnameOrIp = p.Dto.Hostname,
                Location = p.Dto.Location,
                Name = p.Dto.Name,
                ClientsConnectionAllowed = 1
            };

            _fsdServers.Add(fsdServer);
            await _liveFeedProducer.ProduceMessage(p.Dto);
        }

        private async void FsdConsumer_WallopDtoReceived(object sender, DtoReceivedEventArgs<WallopDto> p)
        {
            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.From);
            if (fsdClient == null) return;
            p.Dto.Cid = fsdClient.Cid;
            p.Dto.Realname = fsdClient.Realname;

            Console.WriteLine($"Wallop from {p.Dto.From} - {fsdClient.Realname}");
            await _liveFeedProducer.ProduceMessage(p.Dto);
        }

        private async void FsdConsumer_BroadcastDtoReceived(object sender, DtoReceivedEventArgs<BroadcastDto> p)
        {
            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.From);
            if (fsdClient == null) return;
            p.Dto.Cid = fsdClient.Cid;
            p.Dto.Realname = fsdClient.Realname;

            Console.WriteLine($"Broadcast from {p.Dto.From} - {fsdClient.Realname}");
            await _liveFeedProducer.ProduceMessage(p.Dto);
        }

        private void FsdConsumer_AtisTimerElapsed(object source, ElapsedEventArgs e)
        {
            _fsdConsumer.AddClient();

            foreach (AtisRequestDto atisRequestDto in _fsdClients.Select(fsdClient => new AtisRequestDto(fsdClient.Callsign, ConsumerName, _fsdConsumer.DtoCount, 1, ConsumerCallsign)))
            {
                _fsdConsumer.Client.Write(atisRequestDto + "\r\n");
                _fsdConsumer.DtoCount++;
            }

            _fsdConsumer.DelClient();
        }

        private void RemoveTimedOutConnections(object source, ElapsedEventArgs e)
        {
            _fsdClients.RemoveAll(c => (DateTime.UtcNow - c.LastUpdated).Minutes > 4);
            _fsdPrefiles.RemoveAll(p => (DateTime.UtcNow - p.LastUpdated).Hours > 2);
        }

        private void SetPrometheusConnectionCounts(object source, ElapsedEventArgs e)
        {
            foreach (FsdServer fsdServer in _fsdServers)
            {
                _totalConnections.WithLabels(fsdServer.Name).Set(_fsdClients.Count(c => c.Server == fsdServer.Name));
                _totalAtisConnections.WithLabels("true", fsdServer.Name).Set(_fsdClients.Count(c =>
                    c.Server == fsdServer.Name && c.Callsign.EndsWith("_ATIS") && string.IsNullOrEmpty(c.AtisMessage)));
                _totalAtisConnections.WithLabels("false", fsdServer.Name).Set(_fsdClients.Count(c =>
                    c.Server == fsdServer.Name && c.Callsign.EndsWith("_ATIS") &&
                    !string.IsNullOrEmpty(c.AtisMessage)));
            }

            _uniqueConnections.Set(_fsdClients.GroupBy(c => c.Cid).Select(g => g.FirstOrDefault()).Count());
        }

        private string GenerateDataFileText()
        {
            StringBuilder fileContents = new StringBuilder();
            fileContents.AppendLine(
                "; !CLIENTS section -         callsign:cid:realname:clienttype:frequency:latitude:longitude:altitude:groundspeed:planned_aircraft:planned_tascruise:planned_depairport:planned_altitude:planned_destairport:server:protrevision:rating:transponder:facilitytype:visualrange:planned_revision:planned_flighttype:planned_deptime:planned_actdeptime:planned_hrsenroute:planned_minenroute:planned_hrsfuel:planned_minfuel:planned_altairport:planned_remarks:planned_route:planned_depairport_lat:planned_depairport_lon:planned_destairport_lat:planned_destairport_lon:atis_message:time_last_atis_received:time_logon:heading:QNH_iHg:QNH_Mb:");
            fileContents.AppendLine("!GENERAL:");
            fileContents.AppendLine("VERSION = 8");
            fileContents.AppendLine("RELOAD = 1");
            fileContents.AppendLine($"UPDATE = {DateTime.UtcNow:yyyyMMddHHmmss}");
            fileContents.AppendLine($"CONNECTED CLIENTS = {_fsdClients.Count}");
            fileContents.AppendLine($"UNIQUE USERS = {_fsdClients.GroupBy(c => c.Cid).Select(g => g.FirstOrDefault()).Count()}");
            fileContents.AppendLine(";");
            fileContents.AppendLine(";");
            fileContents.AppendLine("!CLIENTS:");
            foreach (FsdClient fsdClient in _fsdClients.ToList())
            {
                fileContents.AppendLine(

                     $"{fsdClient.Callsign}:{fsdClient.Cid}:{fsdClient.Realname}:{fsdClient.Clienttype}:{fsdClient.Frequency}:{fsdClient.Latitude}:{fsdClient.Longitude}:{fsdClient.Altitude}:{fsdClient.Groundspeed}:{fsdClient.PlannedAircraft}:{fsdClient.PlannedTascruise}:{fsdClient.PlannedDepairport}:{fsdClient.PlannedAltitude}:{fsdClient.PlannedDestairport}:{fsdClient.Server}:{fsdClient.Protrevision}:{fsdClient.Rating}:{fsdClient.Transponder}:{fsdClient.Facilitytype}:{fsdClient.Visualrange}:{fsdClient.PlannedRevision}:{fsdClient.PlannedFlighttype}:{fsdClient.PlannedDeptime}:{fsdClient.PlannedActdeptime}:{fsdClient.PlannedHrsenroute}:{fsdClient.PlannedMinenroute}:{fsdClient.PlannedHrsfuel}:{fsdClient.PlannedMinfuel}:{fsdClient.PlannedAltairport}:{fsdClient.PlannedRemarks}:{fsdClient.PlannedRoute}:{fsdClient.PlannedDepairportLat}:{fsdClient.PlannedDepairportLon}:{fsdClient.PlannedDestairportLat}:{fsdClient.PlannedDestairportLon}:{fsdClient.AtisMessage}:{fsdClient.TimeLastAtisReceived:yyyyMMddHHmmss}:{fsdClient.TimeLogon:yyyyMMddHHmmss}:{fsdClient.Heading}:{fsdClient.QnhIHg}:{fsdClient.QnhMb}:"
                        .Replace("", "").Replace("ï¿½", ""));
            }

            fileContents.AppendLine(";");
            fileContents.AppendLine(";");
            fileContents.AppendLine("!SERVERS:");
            foreach (FsdServer fsdServer in _fsdServers.ToList())
            {
                fileContents.AppendLine($"{fsdServer.Ident}:{fsdServer.HostnameOrIp}:{fsdServer.Location}:{fsdServer.Name}:{fsdServer.ClientsConnectionAllowed}:");
            }

            fileContents.AppendLine(";");
            fileContents.AppendLine(";");
            fileContents.AppendLine("!PREFILE:");
            foreach (FsdClient fsdClient in _fsdPrefiles.ToList())
            {
                fileContents.AppendLine(
                    $"{fsdClient.Callsign}:{fsdClient.Cid}:{fsdClient.Realname}:{fsdClient.Clienttype}:{fsdClient.Frequency}:{fsdClient.Latitude}:{fsdClient.Longitude}:{fsdClient.Altitude}:{fsdClient.Groundspeed}:{fsdClient.PlannedAircraft}:{fsdClient.PlannedTascruise}:{fsdClient.PlannedDepairport}:{fsdClient.PlannedAltitude}:{fsdClient.PlannedDestairport}:{fsdClient.Server}:{fsdClient.Protrevision}:{fsdClient.Rating}:{fsdClient.Transponder}:{fsdClient.Facilitytype}:{fsdClient.Visualrange}:{fsdClient.PlannedRevision}:{fsdClient.PlannedFlighttype}:{fsdClient.PlannedDeptime}:{fsdClient.PlannedActdeptime}:{fsdClient.PlannedHrsenroute}:{fsdClient.PlannedMinenroute}:{fsdClient.PlannedHrsfuel}:{fsdClient.PlannedMinfuel}:{fsdClient.PlannedAltairport}:{fsdClient.PlannedRemarks}:{fsdClient.PlannedRoute}:{fsdClient.PlannedDepairportLat}:{fsdClient.PlannedDepairportLon}:{fsdClient.PlannedDestairportLat}:{fsdClient.PlannedDestairportLon}:{fsdClient.AtisMessage}:{fsdClient.TimeLastAtisReceived:yyyyMMddHHmmss}:{fsdClient.TimeLogon:yyyyMMddHHmmss}:{fsdClient.Heading}:{fsdClient.QnhIHg}:{fsdClient.QnhMb}:"
                        .Replace("", "").Replace("ï¿½", ""));
            }

            return fileContents.ToString();
        }

        private JsonGeneralData GenerateGeneralDataForJson(int version)
        {
            JsonGeneralData generalData = new JsonGeneralData
            {
                Version = version,
                Reload = 1,
                Update = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                UpdateTimestamp = DateTime.UtcNow,
                ConnectedClients = _fsdClients.Count,
                UniqueUsers = _fsdClients.GroupBy(c => c.Cid).Select(g => g.FirstOrDefault()).Count()
            };

            return generalData;
        }

        private void WriteDataFiles(object source, ElapsedEventArgs e)
        {
            try
            {
                // text version
                string contents = GenerateDataFileText();
                PutObjectRequest txtPutRequest = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                    Key = "vatsim-data.txt",
                    ContentBody = contents,
                    CannedACL = S3CannedACL.PublicRead
                };
                AmazonS3Client.PutObjectAsync(txtPutRequest);

                // json file version 1
                JsonFileResourceV1 jsonFileResourcev1 = new JsonFileResourceV1(_fsdClients.ToList(), _fsdServers.ToList(), _fsdPrefiles.ToList(), GenerateGeneralDataForJson(1));
                string jsonv1 = JsonConvert.SerializeObject(jsonFileResourcev1, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                });
                byte[] isoBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(jsonv1);
                byte[] utf8Bytes = Encoding.Convert(Encoding.GetEncoding("ISO-8859-1"), Encoding.UTF8, isoBytes);
                string jsonv1Utf8 = Encoding.UTF8.GetString(utf8Bytes);

                PutObjectRequest jsonPutRequest = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                    Key = "vatsim-data.json",
                    ContentBody = jsonv1Utf8,
                    CannedACL = S3CannedACL.PublicRead
                };
                AmazonS3Client.PutObjectAsync(jsonPutRequest);
            }
            catch (Exception ex)
            {
                SpacesWrites.WithLabels("true").Inc();
                Console.WriteLine(ex);
            }

            try
            {
                // json file version 2
                List<FsdClient> clients = _fsdClients.ToList();

                List<FsdClient> pilots = clients.Where(client => client.Clienttype == "PILOT").ToList();
                List<FsdClient> atc = clients.Where(client => client.Clienttype == "ATC" && !client.Callsign.ToUpper().Contains("_ATIS")).ToList();
                List<FsdClient> atiss = clients.Where(client => client.Clienttype == "ATC" && client.Callsign.ToUpper().Contains("_ATIS")).ToList();

                JsonFileResourceV2 jsonFileResourcev2 = new JsonFileResourceV2(pilots, atc, atiss, _fsdServers, _fsdPrefiles, GenerateGeneralDataForJson(2));
                string jsonv2 = JsonConvert.SerializeObject(jsonFileResourcev2, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                });
                byte[] isoBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(jsonv2);
                byte[] utf8Bytes = Encoding.Convert(Encoding.GetEncoding("ISO-8859-1"), Encoding.UTF8, isoBytes);
                string jsonv2Utf8 = Encoding.UTF8.GetString(utf8Bytes);
                PutObjectRequest jsonPutRequest2 = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                    Key = "vatsim-data-v2.json",
                    ContentBody = jsonv2Utf8,
                    CannedACL = S3CannedACL.PublicRead
                };
                AmazonS3Client.PutObjectAsync(jsonPutRequest2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
