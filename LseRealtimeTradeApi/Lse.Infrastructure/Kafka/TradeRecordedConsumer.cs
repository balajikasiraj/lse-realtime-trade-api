using Confluent.Kafka;
using Lse.Application.Events;
using System;
using System.Text.Json;
using System.Threading;

namespace Lse.Infrastructure.Kafka
{
    public class TradeRecordedConsumer
    {
        private readonly ConsumerConfig _config;
        private readonly string _topic;

        public TradeRecordedConsumer(string bootstrapServers, string groupId, string topic)
        {
            _config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };
            _topic = topic;
        }

        public void Start(CancellationToken ct)
        {
            using var consumer = new ConsumerBuilder<Ignore, string>(_config).Build();
            consumer.Subscribe(_topic);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var cr = consumer.Consume(ct);
                        if (cr?.Message?.Value is null) continue;

                        var ev = JsonSerializer.Deserialize<TradeRecordedEvent>(cr.Message.Value);
                        if (ev is not null)
                        {
                            Console.WriteLine($"Consumed trade event: {ev.TradeId} {ev.Ticker} {ev.Price}");
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"Consume error: {ex.Error.Reason}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally { consumer.Close(); }
        }
    }
}
