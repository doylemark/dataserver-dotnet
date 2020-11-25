using System;
using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VATSIM.Network.Dataserver.Dtos;

namespace VATSIM.Network.Dataserver
{
    public class LiveFeedProducer
    {
        private readonly IProducer<Null, string> _producer;

        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            TypeNameHandling = TypeNameHandling.Objects,
        };

        public LiveFeedProducer(string server, string username, string password)
        {
            ProducerConfig producerConfig = new ProducerConfig
            {
                BootstrapServers = server, SaslUsername = username,
                SaslPassword = password, SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.SaslPlaintext
            };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        public async void ProduceMessage(FsdDto fsdDto)
        {
            try
            {
                string topic = fsdDto is WallopDto ? "datafeed-secure-v1" : "datafeed-v1";
                await _producer.ProduceAsync(topic, new Message<Null, string> {Value = JsonConvert.SerializeObject(fsdDto, _jsonSerializerSettings)});
            }
            catch (ProduceException<Null, string> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            }
        }
    }
}