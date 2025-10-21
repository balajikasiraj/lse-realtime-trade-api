using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using System;

namespace Lse.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            // Register concrete TradeService so decorator can wrap it
            services.AddScoped<Services.TradeService>();

            // Register ITradeService as decorator that uses distributed cache when available
            services.AddScoped<Services.ITradeService>(sp =>
            {
                var inner = sp.GetRequiredService<Services.TradeService>();
                var cache = sp.GetService<IDistributedCache>();
                if (cache == null)
                    return (Services.ITradeService)inner;

                return new Services.CachedTradeService(inner, cache, configuration);
            });

            return services;
        }
    }
}
