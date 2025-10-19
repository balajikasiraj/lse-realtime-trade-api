using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lse.Application.Services;

namespace Lse.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITradeService, TradeService>();

            return services;
        }
    }
}
