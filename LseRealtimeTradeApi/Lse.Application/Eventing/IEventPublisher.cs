using System.Threading;
using System.Threading.Tasks;

namespace Lse.Application.Eventing
{
    public interface IEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default);
    }
}
