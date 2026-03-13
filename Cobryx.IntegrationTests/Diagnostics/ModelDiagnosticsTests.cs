using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using Cobryx.Application.Common.Interfaces;
using Moq;

namespace Cobryx.IntegrationTests.Diagnostics
{
    public class ModelDiagnosticsTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        [Fact]
        public void ExportModelDebugView()
        {
            var options = new DbContextOptionsBuilder<CobryxDbContext>()
                .UseInMemoryDatabase("DiagnosticsDb_" + Guid.NewGuid().ToString())
                .Options;

            var tenantProviderMock = new Mock<ITenantProvider>();
            
            using var context = new CobryxDbContext(options, tenantProviderMock.Object);
            
            var debugView = context.Model.ToDebugString();
            var debugPath = Path.Combine(Environment.CurrentDirectory, "ef_model_debug.txt");
            var indicesPath = Path.Combine(Environment.CurrentDirectory, "ef_model_indices.txt");

            File.WriteAllText(debugPath, debugView);
            
            using var writer = new StreamWriter(indicesPath);
            foreach (var entity in context.Model.GetEntityTypes())
            {
                if (entity.Name.Contains("Lending") || entity.Name.Contains("Credit") || entity.Name.Contains("Loan"))
                {
                    writer.WriteLine($"Entity: {entity.Name}");
                    foreach (var index in entity.GetIndexes())
                    {
                        writer.WriteLine($"  Index: [{string.Join(", ", index.Properties.Select(p => p.Name))}] Name: {index.GetDatabaseName()}");
                    }
                }
            }
        }
    }
}
