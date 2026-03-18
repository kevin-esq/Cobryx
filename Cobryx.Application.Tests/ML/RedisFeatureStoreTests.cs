#pragma warning disable IDE0005
using System.Threading.Tasks;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML;
using Cobryx.Domain.ML;
using Moq;

namespace Cobryx.Application.Tests.ML;

public class RedisFeatureStoreTests
{
    [Fact]
    public async Task FeatureStore_ShouldPersistAndRetrieve()
    {
        var cacheMock = new Mock<ICacheService>();
        var store = new RedisFeatureStore(cacheMock.Object);
        var customerId = Guid.NewGuid();
        var vector = new FeatureVector { PD = 0.5m, Utilization = 0.8m };

        cacheMock.Setup(x => x.GetAsync<FeatureVector>($"features:{customerId}", default)).ReturnsAsync(vector);

        var retrieved = await store.GetAsync(customerId);

        Assert.Equal(0.5m, retrieved.PD);
        Assert.Equal(0.8m, retrieved.Utilization);
        
        await store.SetAsync(customerId, retrieved);
        cacheMock.Verify(x => x.SetAsync($"features:{customerId}", retrieved, TimeSpan.FromHours(6), default), Times.Once);
    }
}
