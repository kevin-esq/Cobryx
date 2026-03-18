using System.Threading.Tasks;

namespace Cobryx.Application.Common.Interfaces;

public interface IEventProducer
{
    Task ProduceAsync<T>(string topic, string key, T message);
}

public interface IEventConsumer
{
    Task ConsumeAsync(string topic, System.Threading.CancellationToken ct);
}
