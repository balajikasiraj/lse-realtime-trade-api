using Lse.Application.Eventing;
using Lse.Domain.Repositories;
using Lse.Infrastructure.Eventing;
using Lse.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lse.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Keep InMemory DB for MVP; configuration can be used to select provider later
            services.AddDbContext<LseDbContext>(opt => opt.UseInMemoryDatabase("LseDb"));

            services.AddScoped<ITradeRepository, TradeRepository>();

            // Redis distributed cache (StackExchange)
            var redisConn = configuration.GetValue<string>("Redis:ConnectionString");
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConn;
                });
            }
            else
            {
                // fallback to in-memory cache for local dev
                services.AddDistributedMemoryCache();
            }

            // Kafka event publisher
            var kafkaBootstrap = configuration.GetValue<string>("Kafka:BootstrapServers");
            var kafkaTopic = configuration.GetValue<string>("Kafka:Topic:TradeRecorded", "trades.recorded");
            if (!string.IsNullOrWhiteSpace(kafkaBootstrap))
            {
                services.AddSingleton<IEventPublisher>(sp => new KafkaEventPublisher(kafkaBootstrap, kafkaTopic));
            }
            else
            {
                services.AddSingleton<IEventPublisher, NoopEventPublisher>();
            }

            return services;
        }

        private class NoopEventPublisher : IEventPublisher
        {
            public System.Threading.Tasks.Task PublishAsync<TEvent>(TEvent @event, System.Threading.CancellationToken ct = default)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }
}