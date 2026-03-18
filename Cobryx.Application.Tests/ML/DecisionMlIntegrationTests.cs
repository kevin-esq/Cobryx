#pragma warning disable IDE0005
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cobryx.Application.Decision;
using Cobryx.Application.ML;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;
using Moq;
using Moq.Protected;

namespace Cobryx.Application.Tests.Decision;

public class DecisionMlIntegrationTests
{
    [Fact]
    public async Task MlPrediction_ShouldInfluenceDecision()
    {
        var mockHttp = new Mock<HttpMessageHandler>();
        mockHttp.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"ProbabilityOfDefault\": 0.9}")
            });

        var httpClient = new HttpClient(mockHttp.Object) { BaseAddress = new Uri("http://dummy") };
        var mlClient = new MlClient(httpClient);

        var featureStoreMock = new Mock<IFeatureStore>();
        featureStoreMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(new FeatureVector { Outstanding = 1000m });

        var engine = new DecisionEngine(new CreditLimitEngine(), new PricingEngine(), new FraudEngine());
        var dbMock = new Mock<Cobryx.Application.Common.Interfaces.ICobryxDbContext>();
        dbMock.Setup(d => d.ModelOutcomes).Returns(new Mock<Microsoft.EntityFrameworkCore.DbSet<ModelOutcome>>().Object);
        dbMock.Setup(d => d.DecisionSnapshots).Returns(new Mock<Microsoft.EntityFrameworkCore.DbSet<DecisionSnapshot>>().Object);

        var cacheMock = new Mock<Cobryx.Application.Common.Interfaces.ICacheService>();
        
        var service = new DecisionService(engine, cacheMock.Object, dbMock.Object, featureStoreMock.Object, mlClient);

        var customerId = Guid.NewGuid();
        var ctx = new DecisionContext
        {
            Credit = new CreditContext { ProbabilityOfDefault = 0.1m, MonthlyIncomeEstimate = 1000m, Utilization = 0m },
            Pricing = new PricingContext { ProbabilityOfDefault = 0.1m },
            Fraud = new FraudContext()
        };

        var result = await service.EvaluateAsync(customerId, ctx);
        
        // Context PD (heuristic) was 0.1m. ML returned 0.9m.
        // Final PD = (0.1 * 0.3) + (0.9 * 0.7) = 0.03 + 0.63 = 0.66m.
        // So CreditEngine limit should be strictly constrained by high risk
        Assert.True(result.InterestRate > 0.15m); // Pricing reflects high risk
    }
}
