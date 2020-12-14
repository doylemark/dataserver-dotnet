using System;
using System.Collections.Generic;
using System.Text;
using Timer = System.Timers.Timer;
using VATSIM.Network.Dataserver.Models.V3;
using VATSIM.Network.Dataserver.Services;
using VATSIM.Network.Dataserver.Dtos;
using System.Timers;
using System.Linq;
using System.Text.RegularExpressions;
using VATSIM.Network.Dataserver.Models;
using Amazon.S3;
using VATSIM.Network.Dataserver.Resources;
using Newtonsoft.Json;
using Amazon.S3.Model;
using Newtonsoft.Json.Serialization;

namespace VATSIM.Network.Dataserver
{
    public class FeedVersion3
    {
        private readonly HttpService _httpService = new HttpService();

        private readonly FsdConsumer _fsdConsumer = new FsdConsumer(Environment.GetEnvironmentVariable("FSD_HOST"), int.Parse(Environment.GetEnvironmentVariable("FSD_PORT") ?? "4113"));
        private const string ConsumerName = "DSERVER3";
        private const string ConsumerCallsign = "DCLIENT3";

        private readonly List<FsdPilot> _fsdPilots = new List<FsdPilot>();
        private readonly List<FsdController> _fsdControllers = new List<FsdController>();
        private readonly List<FsdAtis> _fsdAtiss = new List<FsdAtis>();
        private readonly List<FsdPrefile> _fsdPrefiles = new List<FsdPrefile>();
        private readonly List<FsdServer> _fsdServers = new List<FsdServer>();

        private readonly List<AtcFacility> _facilities = new List<AtcFacility>();
        private readonly List<Rating> _ratings = new List<Rating>();
        private readonly List<PilotRating> _pilotratings = new List<PilotRating>();

        private readonly List<string> _atisicaos = new List<string>();
        private readonly List<string> _atisphonetics = new List<string>();

        private readonly Timer _fileTimer = new Timer(15000);
        private static readonly AmazonS3Client AmazonS3Client = new AmazonS3Client(new AmazonS3Config
        {
            ServiceURL = Environment.GetEnvironmentVariable("S3_URL")
        });

        private readonly Timer _timeoutTimer = new Timer(60000);
        private readonly Timer _pilotRatingTimer = new Timer(60000);
        
        public void StartFeedVersion3()
        {
            PopulateFacilityTypes();
            PopulateRatings();
            PopulatePilotRatings();
            PopulateAtisIcaos();

            _fsdConsumer.NotifyDtoReceived += FsdConsumer_NotifyDtoReceived;
            _fsdConsumer.AddClientDtoReceived += FsdConsumer_AddClientDtoReceived;
            _fsdConsumer.RemoveClientDtoReceived += FsdConsumer_RemoveClientDtoReceived;
            _fsdConsumer.PilotDataDtoReceived += FsdConsumer_PilotDataDtoReceived;
            _fsdConsumer.AtcDataDtoReceived += FsdConsumer_AtcDataDtoReceived;
            _fsdConsumer.FlightPlanDtoReceived += FsdConsumer_FlightPlanDtoReceived;
            _fsdConsumer.FlightPlanCancelDtoReceived += FsdConsumer_FlightPlanCancelDtoReceived;
            _fsdConsumer.AtisDataDtoReceived += FsdConsumer_AtisDataDtoReceived;
            _fsdConsumer.AtisTimer.Elapsed += FsdConsumer_AtisTimerElapsed;
            _timeoutTimer.Elapsed += RemoveTimedOutConnections;
            _fileTimer.Elapsed += WriteDataFiles;
            _pilotRatingTimer.Elapsed += FillPilotRatings;

            _fsdConsumer.Start(ConsumerName, ConsumerCallsign);
            _fsdConsumer.AtisTimer.Start();

            _timeoutTimer.Start();
            _fileTimer.Start();
            _pilotRatingTimer.Start();

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
                    PilotRatingSet = false,
                    HasPilotData = false
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
                    LastUpdated = DateTime.UtcNow,
                    HasControllerData = false
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
                    LastUpdated = DateTime.UtcNow,
                    HasControllerData = false
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
            if (fsdPilot == null) return;
            fsdPilot.HasPilotData = true;
            fsdPilot.Transponder = p.Dto.Transponder.ToString("0000");
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
                if (fsdController == null) return;
                fsdController.HasControllerData = true;
                fsdController.Frequency = p.Dto.Frequency.Insert(2, ".").Insert(0, "1");
                fsdController.Facility = p.Dto.FacilityType;
                fsdController.VisualRange = p.Dto.VisualRange;
                fsdController.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                FsdAtis fsdAtis = _fsdAtiss.Find(c => c.Callsign == p.Dto.Callsign);
                if (fsdAtis == null) return;
                fsdAtis.HasControllerData = true;
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
                    ApiUserData response = await _httpService.GetUserData(p.Dto.Cid);
                    FsdPrefile fsdPrefile = new FsdPrefile
                    {
                        Cid = int.Parse(p.Dto.Cid),
                        Name = $"{response.FirstName} {response.LastName}",
                        Callsign = p.Dto.Callsign,
                        LastUpdated = DateTime.UtcNow,
                        FlightPlan = FillFlightPlanFromDto(p.Dto),
                    };

                    _fsdPrefiles.Add(fsdPrefile);
                }
                else
                {
                    FsdPilot fsdPilot = _fsdPilots.Find(c => c.Callsign == p.Dto.Callsign);
                    if (fsdPilot == null) return;
                    fsdPilot.FlightPlan = FillFlightPlanFromDto(p.Dto);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static FlightPlan FillFlightPlanFromDto(FlightPlanDto dto)
        {
            return new FlightPlan
            {
                FlightRules = dto.Type,
                Aircraft = dto.Aircraft,
                Departure = dto.DepartureAirport,
                Arrival = dto.DestinationAirport,
                Alternate = dto.AlternateAirport,
                CruiseTas = dto.CruiseSpeed,
                Altitude = dto.Altitude,
                Deptime = int.Parse(dto.EstimatedDepartureTime).ToString("0000"),
                EnrouteTime = FormatFsdTime(dto.HoursEnroute, dto.MinutesEnroute),
                FuelTime = FormatFsdTime(dto.HoursFuel, dto.MinutesFuel),
                Remarks = dto.Remarks.ToUpper(),
                Route = dto.Route.ToUpper()
            };
        }

        private void FsdConsumer_FlightPlanCancelDtoReceived(object sender, DtoReceivedEventArgs<FlightPlanCancelDto> p)
        {
            _fsdPrefiles.RemoveAll(c => c.Callsign == p.Dto.Callsign);
        }

        private static string FormatFsdTime(string hours, string minutes)
        {
            if(int.Parse(hours) < 10)
            {
                hours = "0" + hours;
            }
            if(int.Parse(minutes) < 10)
            {
                minutes = "0" + minutes;
            }
            return hours + minutes;
        }

        private void FsdConsumer_AtisDataDtoReceived(object sender, DtoReceivedEventArgs<AtisDataDto> p)
        {
            if (!p.Dto.From.ToUpper().Contains("_ATIS"))
            {
                FsdController fsdController = _fsdControllers.Find(c => c.Callsign == p.Dto.From);
                if (fsdController == null) return;
                switch (p.Dto.Type)
                {
                    case "T" when fsdController.AppendAtis:
                        fsdController.TextAtis.Add(new Regex("[ ]{2,}", RegexOptions.None).Replace(p.Dto.Data.ToUpper(), " ").Trim());
                        break;
                    case "T":
                        fsdController.TextAtis = new List<string>
                        {
                            new Regex("[ ]{2,}", RegexOptions.None).Replace(p.Dto.Data.ToUpper(), " ").Trim()
                        };
                        fsdController.AppendAtis = true;
                        break;
                    case "E":
                        fsdController.AppendAtis = false;
                        break;
                }
            }
            else
            {
                FsdAtis fsdAtis = _fsdAtiss.Find(c => c.Callsign == p.Dto.From);
                if (fsdAtis == null) return;
                switch (p.Dto.Type)
                {
                    case "A":
                        fsdAtis.AtisCode = p.Dto.Data.ToUpper();
                        break;
                    case "T" when fsdAtis.AppendAtis:
                        fsdAtis.TextAtis.Add(new Regex("[ ]{2,}", RegexOptions.None).Replace(p.Dto.Data.ToUpper(), " ").Trim());
                        break;
                    case "T":
                        fsdAtis.TextAtis = new List<string>
                        {
                            new Regex("[ ]{2,}", RegexOptions.None).Replace(p.Dto.Data.ToUpper(), " ").Trim()
                        };
                        fsdAtis.AppendAtis = true;
                        break;
                    case "E":
                        fsdAtis.AppendAtis = false;
                        break;
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
            List<FsdController> controllers = _fsdControllers.ToList();
            List<FsdAtis> atiss = _fsdAtiss.ToList();

            _fsdConsumer.AddClient();

            foreach (AtisRequestDto atisRequestDto in controllers.Select(fsdClient => new AtisRequestDto(fsdClient.Callsign, ConsumerName, _fsdConsumer.DtoCount, 1, ConsumerCallsign)))
            {
                _fsdConsumer.Client.Write(atisRequestDto + "\r\n");
                _fsdConsumer.DtoCount++;
            }

            foreach (AtisRequestDto atisRequestDto in atiss.Select(fsdClient => new AtisRequestDto(fsdClient.Callsign, ConsumerName, _fsdConsumer.DtoCount, 1, ConsumerCallsign)))
            {
                _fsdConsumer.Client.Write(atisRequestDto + "\r\n");
                _fsdConsumer.DtoCount++;
            }

            _fsdConsumer.DelClient();

            RecalculateAtisIcaos();
        }

        private void RecalculateAtisIcaos()
        {
            List<FsdAtis> atiss = _fsdAtiss.ToList();

            foreach (FsdAtis atis in atiss)
            {
                atis.AtisCode = null;
                if (atis.TextAtis == null) continue;

                foreach (string[] strings in from line in atis.TextAtis where atis.AtisCode == null where line.Contains("INFORMATION ") || line.Contains("INFO ") || line.Contains("INFO ") || line.Contains("ATIS ") select line.Split(" "))
                {
                    foreach (string strin in strings)
                    {
                        string clean = strin.Replace(".", string.Empty).Replace(",", string.Empty);
                        if (!_atisphonetics.Contains(clean) && !_atisicaos.Contains(clean)) continue;
                        string letter = strin.Substring(0, 1);
                        
                        FsdAtis fsdAtis = _fsdAtiss.Find(c => c.Callsign == atis.Callsign);
                        if (fsdAtis == null) return;
                        atis.AtisCode = null;
                        fsdAtis.AtisCode = letter;
                        break;
                    }
                }
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
            List<FsdPilot> pilots = _fsdPilots.Where(p => !p.PilotRatingSet).ToList();
            foreach(FsdPilot pilot in pilots)
            {
                try
                {
                    ApiUserData response = await _httpService.GetUserData(pilot.Cid.ToString());
                    FsdPilot fsdPilot = _fsdPilots.Find(c => c.Cid == pilot.Cid);
                    if (fsdPilot == null) return;
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
            List<FsdPilot> pilots = _fsdPilots.Where(p => p.HasPilotData).ToList();
            List<FsdController> controllers = _fsdControllers.ToList();
            List<FsdAtis> atiss = _fsdAtiss.ToList();

            List<int> cids = pilots.Select(pilot => pilot.Cid).ToList();
            cids.AddRange(controllers.Select(controller => controller.Cid));
            cids.AddRange(atiss.Select(atis => atis.Cid));

            JsonGeneralData generalData = new JsonGeneralData
            {
                Version = 3,
                Reload = 1,
                Update = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                UpdateTimestamp = DateTime.UtcNow,
                ConnectedClients = cids.Count,
                UniqueUsers = cids.GroupBy(c => c).Select(g => g.FirstOrDefault()).Count()
            };

            return generalData;
        }

        private void WriteDataFiles(object source, ElapsedEventArgs e)
        {
            try
            {
                // json file version 3
                List<FsdPilot> pilots = _fsdPilots.Where(p => p.HasPilotData).ToList();
                List<FsdController> controllers = _fsdControllers.Where(p => p.HasControllerData).ToList();
                List<FsdAtis> atiss = _fsdAtiss.Where(p => p.HasControllerData).ToList();
                List<FsdPrefile> prefiles = _fsdPrefiles.ToList();

                JsonFileResourceV3 jsonFileResourcev3 = new JsonFileResourceV3(pilots, controllers, atiss, _fsdServers, prefiles, _facilities, _ratings, _pilotratings, GenerateGeneralDataForV3Json());
                string jsonv3 = JsonConvert.SerializeObject(jsonFileResourcev3, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                });
                byte[] isoBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(jsonv3);
                byte[] utf8Bytes = Encoding.Convert(Encoding.GetEncoding("ISO-8859-1"), Encoding.UTF8, isoBytes);
                string jsonv3Utf8 = Encoding.UTF8.GetString(utf8Bytes);
                PutObjectRequest jsonPutRequest3 = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                    Key = "vatsim-data-v3.json",
                    ContentBody = jsonv3Utf8,
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

        private void PopulateAtisIcaos()
        {
            _atisicaos.Add("A");
            _atisicaos.Add("B");
            _atisicaos.Add("C");
            _atisicaos.Add("D");
            _atisicaos.Add("E");
            _atisicaos.Add("F");
            _atisicaos.Add("G");
            _atisicaos.Add("H");
            _atisicaos.Add("I");
            _atisicaos.Add("J");
            _atisicaos.Add("K");
            _atisicaos.Add("L");
            _atisicaos.Add("M");
            _atisicaos.Add("N");
            _atisicaos.Add("O");
            _atisicaos.Add("P");
            _atisicaos.Add("Q");
            _atisicaos.Add("R");
            _atisicaos.Add("S");
            _atisicaos.Add("T");
            _atisicaos.Add("U");
            _atisicaos.Add("V");
            _atisicaos.Add("W");
            _atisicaos.Add("X");
            _atisicaos.Add("Y");
            _atisicaos.Add("Z");
            _atisphonetics.Add("ALPHA");
            _atisphonetics.Add("BRAVO");
            _atisphonetics.Add("CHARLIE");
            _atisphonetics.Add("DELTA");
            _atisphonetics.Add("ECHO");
            _atisphonetics.Add("FOXTROT");
            _atisphonetics.Add("GOLF");
            _atisphonetics.Add("HOTEL");
            _atisphonetics.Add("INDIA");
            _atisphonetics.Add("JULIET");
            _atisphonetics.Add("JULIETT");
            _atisphonetics.Add("KILO");
            _atisphonetics.Add("LIMA");
            _atisphonetics.Add("MIKE");
            _atisphonetics.Add("NOVEMBER");
            _atisphonetics.Add("OSCAR");
            _atisphonetics.Add("PAPA");
            _atisphonetics.Add("QUEBEC");
            _atisphonetics.Add("ROMEO");
            _atisphonetics.Add("SIERRA");
            _atisphonetics.Add("TANGO");
            _atisphonetics.Add("UNIFORM");
            _atisphonetics.Add("VICTOR");
            _atisphonetics.Add("WHISKEY");
            _atisphonetics.Add("XRAY");
            _atisphonetics.Add("YANKEE");
            _atisphonetics.Add("ZULU");
        }
    }
}
