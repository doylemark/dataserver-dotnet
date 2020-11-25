using System;
using System.Collections.Generic;
using System.Text;
using Timer = System.Timers.Timer;
using VATSIM.Network.Dataserver.Models.V3;
using VATSIM.Network.Dataserver.Services;
using VATSIM.Network.Dataserver.Dtos;
using System.Timers;
using System.Linq;
using VATSIM.Network.Dataserver.Models;
using Prometheus;
using System.Threading.Tasks;
using Amazon.S3;
using VATSIM.Network.Dataserver.Resources;
using Newtonsoft.Json;
using Amazon.S3.Model;
using Newtonsoft.Json.Serialization;

namespace VATSIM.Network.Dataserver
{
    public class FeedVersion3
    {
        private readonly HttpService httpService = new HttpService();

        private readonly FsdConsumer FsdConsumer = new FsdConsumer(Environment.GetEnvironmentVariable("FSD_HOST"), int.Parse(Environment.GetEnvironmentVariable("FSD_PORT")));
        public readonly string ConsumerName = "DSERVER3";
        public readonly string ConsumerCallsign = "DCLIENT3";

        private readonly List<FsdPilot> _fsdPilots = new List<FsdPilot>();
        private readonly List<FsdController> _fsdControllers = new List<FsdController>();
        private readonly List<FsdAtis> _fsdAtiss = new List<FsdAtis>();
        private readonly List<FsdPrefile> _fsdPrefiles = new List<FsdPrefile>();
        private readonly List<FsdServer> _fsdServers = new List<FsdServer>();

        private readonly List<AtcFacility> _facilities = new List<AtcFacility>();
        private readonly List<Rating> _ratings = new List<Rating>();
        private readonly List<PilotRating> _pilotratings = new List<PilotRating>();

        private static readonly Timer FileTimer = new Timer(15000);
        private static readonly AmazonS3Client AmazonS3Client = new AmazonS3Client(new AmazonS3Config
        {
            ServiceURL = "https://sfo2.digitaloceanspaces.com"
        });

        private readonly Timer TimeoutTimer = new Timer(60000);
        private readonly Timer PilotRatingTimer = new Timer(60000);
        
        public void StartFeedVersion3()
        {
            PopulateFacilityTypes();
            PopulateRatings();
            PopulatePilotRatings();

            FsdConsumer.NotifyDtoReceived += FsdConsumer_NotifyDtoReceived;
            FsdConsumer.AddClientDtoReceived += FsdConsumer_AddClientDtoReceived;
            FsdConsumer.RemoveClientDtoReceived += FsdConsumer_RemoveClientDtoReceived;
            FsdConsumer.PilotDataDtoReceived += FsdConsumer_PilotDataDtoReceived;
            FsdConsumer.AtcDataDtoReceived += FsdConsumer_AtcDataDtoReceived;
            FsdConsumer.FlightPlanDtoReceived += FsdConsumer_FlightPlanDtoReceived;
            FsdConsumer.FlightPlanCancelDtoReceived += FsdConsumer_FlightPlanCancelDtoReceived;
            FsdConsumer.AtisDataDtoReceived += FsdConsumer_AtisDataDtoReceived;
            FsdConsumer.AtisTimer.Elapsed += FsdConsumer_AtisTimerElapsed;
            TimeoutTimer.Elapsed += RemoveTimedOutConnections;
            FileTimer.Elapsed += WriteDataFiles;
            PilotRatingTimer.Elapsed += FillPilotRatings;

            FsdConsumer.Start(ConsumerName, ConsumerCallsign);
            FsdConsumer.AtisTimer.Start();

            TimeoutTimer.Start();
            FileTimer.Start();
            PilotRatingTimer.Start();

            Console.WriteLine("Starting Feed Version 3");
        }

        private void FsdConsumer_AddClientDtoReceived(object sender, DtoReceivedEventArgs<AddClientDto> p)
        {
            if (_fsdPilots.Any(c => c.Callsign == p.Dto.Callsign) || _fsdControllers.Any(c => c.Callsign == p.Dto.Callsign) || _fsdAtiss.Any(c => c.Callsign == p.Dto.Callsign) || p.Dto.Callsign == "AFVDATA" || p.Dto.Callsign == "SUP" || p.Dto.Callsign == "DATA" || p.Dto.Callsign == "DATASVR" || p.Dto.Callsign.Contains("DCLIENT") || p.Dto.Callsign == "DATA-TOR")
            {
                return;
            }

            if (p.Dto.Type == 1)
            {
                FsdPilot fsdPilot = new FsdPilot
                {
                    Cid = int.Parse(p.Dto.Cid),
                    Name = p.Dto.RealName,
                    Callsign = p.Dto.Callsign,
                    Server = p.Dto.Server,
                    LogonTime = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    PilotRatingSet = false
                };
                _fsdPilots.Add(fsdPilot);
                _fsdPrefiles.RemoveAll(f => f.Callsign == p.Dto.Callsign);
            } else if (p.Dto.Type != 1 && !p.Dto.Callsign.ToUpper().Contains("_ATIS"))
            {
                FsdController fsdController = new FsdController
                {
                    Cid = int.Parse(p.Dto.Cid),
                    Name = p.Dto.RealName,
                    Callsign = p.Dto.Callsign,
                    Server = p.Dto.Server,
                    Rating = p.Dto.Rating,
                    LogonTime = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                _fsdControllers.Add(fsdController);

            } else if (p.Dto.Type != 1 && p.Dto.Callsign.ToUpper().Contains("_ATIS"))
            {
                FsdAtis fsdAtis = new FsdAtis
                {
                    Cid = int.Parse(p.Dto.Cid),
                    Callsign = p.Dto.Callsign,
                    Name = p.Dto.RealName,
                    Rating = p.Dto.Rating,
                    Server = p.Dto.Server,
                    LogonTime = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                _fsdAtiss.Add(fsdAtis);
            }
        }

        private void FsdConsumer_RemoveClientDtoReceived(object sender, DtoReceivedEventArgs<RemoveClientDto> p)
        {
            _fsdPilots.RemoveAll(c => c.Callsign == p.Dto.Callsign);
            _fsdControllers.RemoveAll(c => c.Callsign == p.Dto.Callsign);
            _fsdAtiss.RemoveAll(c => c.Callsign == p.Dto.Callsign);
        }

        private void FsdConsumer_PilotDataDtoReceived(object sender, DtoReceivedEventArgs<PilotDataDto> p)
        {
            FsdPilot fsdPilot = _fsdPilots.Find(c => c.Callsign == p.Dto.Callsign);
            fsdPilot.Transponder = p.Dto.Transponder;
            fsdPilot.Latitude = p.Dto.Latitude;
            fsdPilot.Longitude = p.Dto.Longitude;
            fsdPilot.Altitude = p.Dto.Altitude;
            fsdPilot.Groundspeed = p.Dto.GroundSpeed;
            fsdPilot.Heading = p.Dto.Heading;
            fsdPilot.QnhIHg = Math.Round(29.92 - (p.Dto.PressureDifference / 1000.0), 2);
            fsdPilot.QnhMb = (int)Math.Round(fsdPilot.QnhIHg * 33.864);
            fsdPilot.LastUpdated = DateTime.UtcNow;
        }

        private void FsdConsumer_AtcDataDtoReceived(object sender, DtoReceivedEventArgs<AtcDataDto> p)
        {
            if (p.Dto.Callsign == "AFVDATA" || p.Dto.Callsign == "SUP" || p.Dto.Callsign == "DATA" || p.Dto.Callsign == "DATASVR" || p.Dto.Callsign.Contains("DCLIENT") || p.Dto.Callsign == "DATA-TOR")
            {
                return;
            }

            if (!p.Dto.Callsign.ToUpper().Contains("_ATIS"))
            {
                FsdController fsdController = _fsdControllers.Find(c => c.Callsign == p.Dto.Callsign);
                fsdController.Frequency = p.Dto.Frequency.Insert(2, ".").Insert(0, "1");
                fsdController.Facility = p.Dto.FacilityType;
                fsdController.VisualRange = p.Dto.VisualRange;
                fsdController.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                FsdAtis fsdAtis = _fsdAtiss.Find(c => c.Callsign == p.Dto.Callsign);
                fsdAtis.Frequency = p.Dto.Frequency.Insert(2, ".").Insert(0, "1");
                fsdAtis.Facility = p.Dto.FacilityType;
                fsdAtis.VisualRange = p.Dto.VisualRange;
                fsdAtis.LastUpdated = DateTime.UtcNow;
            }
        }

        private async void FsdConsumer_FlightPlanDtoReceived(object sender, DtoReceivedEventArgs<FlightPlanDto> p)
        {
            try
            {
                if (_fsdPilots.All(c => c.Callsign != p.Dto.Callsign))
                {
                    ApiUserData response = await httpService.GetUserData(p.Dto.Cid);
                    FsdPrefile fsdPilot = new FsdPrefile
                    {
                        Cid = int.Parse(p.Dto.Cid),
                        Name = $"{response.FirstName} {response.LastName}",
                        Callsign = p.Dto.Callsign,
                        LastUpdated = DateTime.UtcNow,
                    };

                    fsdPilot.FlightPlan = new FlightPlan
                    {
                        FlightRules = p.Dto.Type,
                        Aircraft = p.Dto.Aircraft,
                        Departure = p.Dto.DepartureAirport,
                        Arrival = p.Dto.DestinationAirport,
                        Alternate = p.Dto.AlternateAirport,
                        CruiseTas = p.Dto.CruiseSpeed,
                        Altitude = p.Dto.Altitude,
                        Deptime = p.Dto.EstimatedDepartureTime,
                        EnrouteTime = FormatFsdTime(p.Dto.HoursEnroute, p.Dto.MinutesEnroute),
                        FuelTime = FormatFsdTime(p.Dto.HoursFuel, p.Dto.MinutesFuel),
                        Remarks = p.Dto.Remarks,
                        Route = p.Dto.Route
                    };

                    _fsdPrefiles.Add(fsdPilot);
                }
                else
                {
                    FsdPilot fsdPilot = _fsdPilots.Find(c => c.Callsign == p.Dto.Callsign);
                    fsdPilot.FlightPlan = new FlightPlan
                    {
                        FlightRules = p.Dto.Type,
                        Aircraft = p.Dto.Aircraft,
                        Departure = p.Dto.DepartureAirport,
                        Arrival = p.Dto.DestinationAirport,
                        Alternate = p.Dto.AlternateAirport,
                        CruiseTas = p.Dto.CruiseSpeed,
                        Altitude = p.Dto.Altitude,
                        Deptime = p.Dto.EstimatedDepartureTime,
                        EnrouteTime = FormatFsdTime(p.Dto.HoursEnroute, p.Dto.MinutesEnroute),
                        FuelTime = FormatFsdTime(p.Dto.HoursFuel, p.Dto.MinutesFuel),
                        Remarks = p.Dto.Remarks,
                        Route = p.Dto.Route
                    };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void FsdConsumer_FlightPlanCancelDtoReceived(object sender, DtoReceivedEventArgs<FlightPlanCancelDto> p)
        {
            _fsdPrefiles.RemoveAll(c => c.Callsign == p.Dto.Callsign);
        }

        private string FormatFsdTime(string Hours, string Minutes)
        {
            if(int.Parse(Hours) < 10)
            {
                Hours = "0" + Hours;
            }
            if(int.Parse(Minutes) < 10)
            {
                Minutes = "0" + Minutes;
            }
            return Hours + Minutes;
        }

        private void FsdConsumer_AtisDataDtoReceived(object sender, DtoReceivedEventArgs<AtisDataDto> p)
        {
            if (!p.Dto.From.ToUpper().Contains("_ATIS"))
            {
                FsdController fsdController = _fsdControllers.Find(c => c.Callsign == p.Dto.From);
                if (p.Dto.Type == "T")
                {
                    if (fsdController.AppendAtis)
                    {
                        fsdController.TextAtis.Add(p.Dto.Data);
                    }
                    else
                    {
                        fsdController.TextAtis = new List<string>
                        {
                            p.Dto.Data
                        };
                        fsdController.AppendAtis = true;
                    }
                }
                else if (p.Dto.Type == "E")
                {
                    fsdController.AppendAtis = false;
                }
            }
            else
            {
                FsdAtis fsdAtis = _fsdAtiss.Find(c => c.Callsign == p.Dto.From);
                if (p.Dto.Type == "T")
                {
                    if (fsdAtis.AppendAtis)
                    {
                        fsdAtis.TextAtis.Add(p.Dto.Data);
                    }
                    else
                    {
                        fsdAtis.TextAtis = new List<string>
                        {
                            p.Dto.Data
                        };
                        fsdAtis.AppendAtis = true;
                    }
                }
                else if (p.Dto.Type == "E")
                {
                    fsdAtis.AppendAtis = false;
                }
            }
        }

        private void FsdConsumer_NotifyDtoReceived(object sender, DtoReceivedEventArgs<NotifyDto> p)
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
        }

        private void FsdConsumer_AtisTimerElapsed(object source, ElapsedEventArgs e)
        {
            foreach (AtisRequestDto atisRequestDto in _fsdControllers.Select(fsdClient => new AtisRequestDto(fsdClient.Callsign, ConsumerName, FsdConsumer.DtoCount, 1, ConsumerCallsign)))
            {
                FsdConsumer.Client.Write(atisRequestDto + "\r\n");
                FsdConsumer.DtoCount++;
            }

            foreach (AtisRequestDto atisRequestDto in _fsdAtiss.Select(fsdClient => new AtisRequestDto(fsdClient.Callsign, ConsumerName, FsdConsumer.DtoCount, 1, ConsumerCallsign)))
            {
                FsdConsumer.Client.Write(atisRequestDto + "\r\n");
                FsdConsumer.DtoCount++;
            }
        }

        private void RemoveTimedOutConnections(object source, ElapsedEventArgs e)
        {
            _fsdControllers.RemoveAll(c => (DateTime.UtcNow - c.LastUpdated).Minutes > 4);
            _fsdAtiss.RemoveAll(c => (DateTime.UtcNow - c.LastUpdated).Minutes > 4);
            _fsdPilots.RemoveAll(c => (DateTime.UtcNow - c.LastUpdated).Minutes > 4);
            _fsdPrefiles.RemoveAll(p => (DateTime.UtcNow - p.LastUpdated).Hours > 2);
        }

        private async void FillPilotRatings(object source, ElapsedEventArgs e)
        {
            List<FsdPilot> _pilots = _fsdPilots.Where(p => p.PilotRatingSet == false).ToList();
            foreach(FsdPilot pilot in _pilots)
            {
                try
                {
                    ApiUserData response = await httpService.GetUserData(pilot.Cid.ToString());
                    FsdPilot fsdPilot = _fsdPilots.Find(c => c.Cid == pilot.Cid);
                    fsdPilot.PilotRating = response.PilotRating;
                    fsdPilot.PilotRatingSet = true;
                }
                catch (Exception excep)
                {
                    Console.WriteLine(excep);
                }
            }
        }

        private JsonGeneralData GenerateGeneralDataForV3Json()
        {
            List<int> _cids = new List<int>();
            List<FsdPilot> _pilots = _fsdPilots.ToList();
            List<FsdController> _controller = _fsdControllers.ToList();
            List<FsdAtis> _atis = _fsdAtiss.ToList();

            foreach (FsdPilot pilot in _pilots)
            {
                _cids.Add(pilot.Cid);
            }
            foreach(FsdController controller in _controller)
            {
                _cids.Add(controller.Cid);
            }
            foreach(FsdAtis atis in _atis)
            {
                _cids.Add(atis.Cid);
            }

            JsonGeneralData generalData = new JsonGeneralData
            {
                Version = 3,
                Reload = 1,
                Update = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                UpdateTimestamp = DateTime.UtcNow,
                ConnectedClients = _cids.Count,
                UniqueUsers = _cids.GroupBy(c => c).Select(g => g.FirstOrDefault()).Count()
            };

            return generalData;
        }

        private void WriteDataFiles(object source, ElapsedEventArgs e)
        {
            try
            {
                // json file version 3
                List<FsdPilot> _pilots = _fsdPilots.ToList();
                List<FsdController> _controller = _fsdControllers.ToList();
                List<FsdAtis> _atis = _fsdAtiss.ToList();
                List<FsdPrefile> _prefiles = _fsdPrefiles.ToList();

                JsonFileResourceV3 jsonFileResourcev3 = new JsonFileResourceV3(_pilots, _controller, _atis, _fsdServers, _prefiles, _facilities, _ratings, _pilotratings, GenerateGeneralDataForV3Json());
                string jsonv3 = JsonConvert.SerializeObject(jsonFileResourcev3, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                });
                byte[] isoBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(jsonv3);
                byte[] utf8Bytes = Encoding.Convert(Encoding.GetEncoding("ISO-8859-1"), Encoding.UTF8, isoBytes);
                string jsonv3utf8 = Encoding.UTF8.GetString(utf8Bytes);
                PutObjectRequest jsonPutRequest3 = new PutObjectRequest
                {
                    BucketName = "vatsim-data-us",
                    Key = "vatsim-data-v3.json",
                    ContentBody = jsonv3utf8,
                    CannedACL = S3CannedACL.PublicRead
                };
                AmazonS3Client.PutObjectAsync(jsonPutRequest3);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void PopulateFacilityTypes()
        {
            _facilities.Add(new AtcFacility { Id = 0, Short = "OBS", Long = "Observer" });
            _facilities.Add(new AtcFacility { Id = 1, Short = "FSS", Long = "Flight Service Station" });
            _facilities.Add(new AtcFacility { Id = 2, Short = "DEL", Long = "Clearance Delivery" });
            _facilities.Add(new AtcFacility { Id = 3, Short = "GND", Long = "Ground" });
            _facilities.Add(new AtcFacility { Id = 4, Short = "TWR", Long = "Tower" });
            _facilities.Add(new AtcFacility { Id = 5, Short = "APP", Long = "Approach/Departure" });
            _facilities.Add(new AtcFacility { Id = 6, Short = "CTR", Long = "Enroute" });
        }

        private void PopulateRatings()
        {
            _ratings.Add(new Rating { Id = -1, Short = "INAC", Long = "Inactive" });
            _ratings.Add(new Rating { Id = 0, Short = "SUS", Long = "Suspended" });
            _ratings.Add(new Rating { Id = 1, Short = "OBS", Long = "Observer" });
            _ratings.Add(new Rating { Id = 2, Short = "S1", Long = "Tower Trainee" });
            _ratings.Add(new Rating { Id = 3, Short = "S2", Long = "Tower Controller" });
            _ratings.Add(new Rating { Id = 4, Short = "S3", Long = "Senior Student" });
            _ratings.Add(new Rating { Id = 5, Short = "C1", Long = "Enroute Controller" });
            _ratings.Add(new Rating { Id = 6, Short = "C2", Long = "Controller 2 (not in use)" });
            _ratings.Add(new Rating { Id = 7, Short = "C3", Long = "Senior Controller" });
            _ratings.Add(new Rating { Id = 8, Short = "I1", Long = "Instructor" });
            _ratings.Add(new Rating { Id = 9, Short = "I2", Long = "Instructor 2 (not in use)" });
            _ratings.Add(new Rating { Id = 10, Short = "I3", Long = "Senior Instructor" });
            _ratings.Add(new Rating { Id = 11, Short = "SUP", Long = "Supervisor" });
            _ratings.Add(new Rating { Id = 12, Short = "ADM", Long = "Administrator" });
        }

        private void PopulatePilotRatings()
        {
            _pilotratings.Add(new PilotRating { Id = 0, ShortName = "NEW", LongName = "Basic Member" });
            _pilotratings.Add(new PilotRating { Id = 1, ShortName = "PPL", LongName = "Private Pilot Licence" });
            _pilotratings.Add(new PilotRating { Id = 3, ShortName = "IR", LongName = "Instrument Rating" });
            _pilotratings.Add(new PilotRating { Id = 7, ShortName = "CMEL", LongName = "Commercial Multi-Engine License" });
            _pilotratings.Add(new PilotRating { Id = 15, ShortName = "ATPL", LongName = "Airline Transport Pilot License" });
        }
    }
}
