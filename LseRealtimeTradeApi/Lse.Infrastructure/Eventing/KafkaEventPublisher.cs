using Confluent.Kafka;
using Lse.Application.Eventing;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lse.Infrastructure.Eventing
{
    public class KafkaEventPublisher : IEventPublisher
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;

        public KafkaEventPublisher(string bootstrapServers, string topic)
        {
            var conf = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<Null, string>(conf).Build();
            _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(@event);
            var msg = new Message<Null, string> { Value = json };
            try
            {
                await _producer.ProduceAsync(_topic, msg, ct).ConfigureAwait(false);
            }
            catch
            {
                // swallow - publishing should not break write path
            }
        }
    }
}
