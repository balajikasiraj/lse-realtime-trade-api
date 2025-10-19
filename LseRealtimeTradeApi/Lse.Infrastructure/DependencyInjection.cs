using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lse.Infrastructure.Repositories;

namespace Lse.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Keep InMemory DB for MVP; configuration can be used to select provider later
            services.AddDbContext<LseDbContext>(opt => opt.UseInMemoryDatabase("LseDb"));

            services.AddScoped<ITradeRepository, TradeRepository>();

            return services;
        }
    }
}
