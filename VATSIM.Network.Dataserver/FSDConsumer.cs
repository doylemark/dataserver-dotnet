using System;
using System.Timers;
using SimpleTCP;
using VATSIM.Network.Dataserver.Dtos;
using VATSIM.Network.Dataserver.Services;
using Timer = System.Timers.Timer;

namespace VATSIM.Network.Dataserver
{
    public class FsdConsumer
    {
        public SimpleTcpClient Client { get; } = new SimpleTcpClient();
        public int DtoCount { get; set; } = 1;
        public string ConsumerName { get; set; } = "";
        public string ConsumerCallsign { get; set; } = "";

        private readonly string _host;
        private readonly int _port;
        public event EventHandler<DtoReceivedEventArgs<AddClientDto>> AddClientDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<RemoveClientDto>> RemoveClientDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<PilotDataDto>> PilotDataDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<AtcDataDto>> AtcDataDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<FlightPlanDto>> FlightPlanDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<FlightPlanCancelDto>> FlightPlanCancelDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<PingDto>> PingDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<AtisDataDto>> AtisDataDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<NotifyDto>> NotifyDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<WallopDto>> WallopDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<BroadcastDto>> BroadcastDtoReceived;
        private readonly Timer _serverTimer = new Timer(60000);
        private readonly Timer _clientTimer = new Timer(5000);
        public Timer AtisTimer { get; } = new Timer(30000);

        public FsdConsumer(string host, int port)
        {
            _host = host;
            _port = port;
            Client.StringEncoder = System.Text.Encoding.GetEncoding("iso-8859-1");
            Client.Delimiter = 10;
            Client.DelimiterDataReceived += client_DelimiterDataReceived;
            PingDtoReceived += fsdConsumer_PingDtoReceived;
            _serverTimer.Elapsed += fsdConsumer_ServerTimerElapsed;
            _clientTimer.Elapsed += fsdConsumer_ClientTimerElapsed;
        }

        public void Start(string name, string callsign)
        {
            ConsumerName = name;
            ConsumerCallsign = callsign;

            Client.Connect(_host, _port);
            Client.Write($"SYNC:*:{ConsumerName}:B{DtoCount}:1:\r\n");
            _serverTimer.Start();
            _clientTimer.Start();

            Console.WriteLine("FSD Consumer Started");
        }

        private void client_DelimiterDataReceived(object sender, Message packet)
        {
            try
            {
                string[] fields = packet.MessageString.Replace("\r", "").Split(":", 2);
                switch (fields[0])
                {
                    case AddClientDto.Packet:
                        AddClientDto addClientDto = AddClientDto.Deserialize(fields[1]);
                        OnAddClientDtoReceived(new DtoReceivedEventArgs<AddClientDto>(addClientDto));
                        break;
                    case RemoveClientDto.Packet:
                        RemoveClientDto removeClientDto = RemoveClientDto.Deserialize(fields[1]);
                        OnRemoveClientDtoReceived(new DtoReceivedEventArgs<RemoveClientDto>(removeClientDto));
                        break;
                    case PilotDataDto.Packet:
                        PilotDataDto pilotDataDto = PilotDataDto.Deserialize(fields[1]);
                        OnPilotDataDtoReceived(new DtoReceivedEventArgs<PilotDataDto>(pilotDataDto));
                        break;
                    case AtcDataDto.Packet:
                        AtcDataDto atcDataDto = AtcDataDto.Deserialize(fields[1]);
                        OnAtcDataDtoReceived(new DtoReceivedEventArgs<AtcDataDto>(atcDataDto));
                        break;
                    case FlightPlanDto.Packet:
                        FlightPlanDto flightPlanDto = FlightPlanDto.Deserialize(fields[1]);
                        OnFlightPlanDtoReceived(new DtoReceivedEventArgs<FlightPlanDto>(flightPlanDto));
                        break;
                    case PingDto.Packet:
                        PingDto pingDto = PingDto.Deserialize(fields[1]);
                        OnPingDtoReceived(new DtoReceivedEventArgs<PingDto>(pingDto));
                        break;
                    case FlightPlanCancelDto.Packet:
                        FlightPlanCancelDto flighplanCancelDto = FlightPlanCancelDto.Deserialize(fields[1]);
                        OnFlightPlanCancelDtoReceived(new DtoReceivedEventArgs<FlightPlanCancelDto>(flighplanCancelDto));
                        break;
                    case NotifyDto.Packet:
                        NotifyDto notifyDto = NotifyDto.Deserialize(fields[1]);
                        OnNotifyDtoReceived(new DtoReceivedEventArgs<NotifyDto>(notifyDto));
                        break;
                    case "MC":
                        fields = packet.MessageString.Replace("\r", "").Split(":");
                        if (fields[5] == "25")
                        {
                            AtisDataDto atisDataDto = AtisDataDto.Deserialize(fields);
                            OnAtisDataDtoReceived(new DtoReceivedEventArgs<AtisDataDto>(atisDataDto));
                        }
                        else if (fields[5] == "5" && fields[1] == "*S")
                        {
                            WallopDto wallopDto = WallopDto.Deserialize(fields);
                            OnWallopDtoReceived(new DtoReceivedEventArgs<WallopDto>(wallopDto));
                        }
                        else if (fields[5] == "5" && fields[1] == "*")
                        {
                            BroadcastDto broadcastDto = BroadcastDto.Deserialize(fields);
                            OnBroadcastDtoReceived(new DtoReceivedEventArgs<BroadcastDto>(broadcastDto));
                        }

                        break;
                    default:
                        // Not a DTO we need to handle...
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected virtual void OnAddClientDtoReceived(DtoReceivedEventArgs<AddClientDto> e)
        {
            EventHandler<DtoReceivedEventArgs<AddClientDto>> handler = AddClientDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnRemoveClientDtoReceived(DtoReceivedEventArgs<RemoveClientDto> e)
        {
            EventHandler<DtoReceivedEventArgs<RemoveClientDto>> handler = RemoveClientDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnPilotDataDtoReceived(DtoReceivedEventArgs<PilotDataDto> e)
        {
            EventHandler<DtoReceivedEventArgs<PilotDataDto>> handler = PilotDataDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnAtcDataDtoReceived(DtoReceivedEventArgs<AtcDataDto> e)
        {
            EventHandler<DtoReceivedEventArgs<AtcDataDto>> handler = AtcDataDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnFlightPlanDtoReceived(DtoReceivedEventArgs<FlightPlanDto> e)
        {
            EventHandler<DtoReceivedEventArgs<FlightPlanDto>> handler = FlightPlanDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnPingDtoReceived(DtoReceivedEventArgs<PingDto> e)
        {
            EventHandler<DtoReceivedEventArgs<PingDto>> handler = PingDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnAtisDataDtoReceived(DtoReceivedEventArgs<AtisDataDto> e)
        {
            EventHandler<DtoReceivedEventArgs<AtisDataDto>> handler = AtisDataDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnNotifyDtoReceived(DtoReceivedEventArgs<NotifyDto> e)
        {
            EventHandler<DtoReceivedEventArgs<NotifyDto>> handler = NotifyDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnWallopDtoReceived(DtoReceivedEventArgs<WallopDto> e)
        {
            EventHandler<DtoReceivedEventArgs<WallopDto>> handler = WallopDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnBroadcastDtoReceived(DtoReceivedEventArgs<BroadcastDto> e)
        {
            EventHandler<DtoReceivedEventArgs<BroadcastDto>> handler = BroadcastDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnFlightPlanCancelDtoReceived(DtoReceivedEventArgs<FlightPlanCancelDto> e)
        {
            EventHandler<DtoReceivedEventArgs<FlightPlanCancelDto>> handler = FlightPlanCancelDtoReceived;
            handler?.Invoke(this, e);
        }

        private void fsdConsumer_PingDtoReceived(object sender, DtoReceivedEventArgs<PingDto> e)
        {
            PongDto pongDto = new PongDto(e.Dto.Source, ConsumerName, DtoCount, 1, e.Dto.Data);
            Client.Write(pongDto + "\r\n");
            DtoCount++;
        }

        private void fsdConsumer_ServerTimerElapsed(object source, ElapsedEventArgs e)
        {
            Client.Write($"SYNC:*:{ConsumerName}:B{DtoCount}:1:\r\n");
            DtoCount++;
            NotifyDto notifyDto = new NotifyDto("*", ConsumerName, DtoCount, 1, 0, ConsumerName, ConsumerName,
                "vpdev@vatsim.net", "127.0.0.1",
                "v1.0", 0, "Toronto, Ontario");
            Client.Write(notifyDto + "\r\n");
            DtoCount++;
        }

        private void fsdConsumer_ClientTimerElapsed(object source, ElapsedEventArgs e)
        {
            AddClientDto addClientDto =
                new AddClientDto("*", ConsumerName, DtoCount, 1, "0", ConsumerName, ConsumerCallsign, 2, 1, 100, ConsumerCallsign, -1, 1);
            Client.Write(addClientDto + "\r\n");
            DtoCount++;
            AtcDataDto atcDataDto = new AtcDataDto("*", ConsumerName, DtoCount, 1, ConsumerCallsign, "99999", 1, 100, 1, 0.00000,
                0.00000);
            Client.Write(atcDataDto + "\r\n");
            DtoCount++;
        }
    }
}