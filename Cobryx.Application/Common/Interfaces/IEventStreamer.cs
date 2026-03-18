namespace Cobryx.Application.Common.Interfaces;

public interface IEventProducer
{
    public Task ProduceAsync<T>(string topic, string key, T message);
}

public interface IEventConsumer
{
    public Task ConsumeAsync(string topic, System.Threading.CancellationToken ct);
}
