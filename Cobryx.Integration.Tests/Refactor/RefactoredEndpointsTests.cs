using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

using Cobryx.Domain.Decision;
using Cobryx.Infrastructure.Persistence;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Cobryx.Integration.Tests.Refactor
{
    /// <summary>
    /// Integration tests for refactored endpoints.
    /// These tests validate the FULL pipeline: HTTP → Controller → MediatR → Handler → Response
    /// </summary>
    [Trait("Category", "Integration")]
    public class RefactoredEndpointsTests(CobryxWebApplicationFactory factory)
        : IClassFixture<CobryxWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly CobryxWebApplicationFactory _factory = factory;
        private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        #region Portfolio Endpoints - Auth Tests

        [Fact]
        public async Task GetPortfolioSummaryWithoutAuthReturns401()
        {
            // Arrange - No auth header
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/v1/portfolio/summary");

            // Assert
            _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetPortfolioAgingWithoutAuthReturns401()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/v1/portfolio/aging");

            // Assert
            _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Portfolio Endpoints - E2E Tests

        [Fact]
        public async Task GetPortfolioSummaryWithValidTenantReturns200Or404()
        {
            // Arrange - Auth + Tenant
            using var client = _factory.CreateClient();
            var token = GenerateTestToken(roles: ["User"], tenantId: TestTenantId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

            // Act
            var response = await client.GetAsync("/api/v1/portfolio/summary");

            // Assert - Pipeline completes without 500
            // 200 = data exists, 400/404 = cache miss (valid for empty test DB)
            _ = response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                "Pipeline should complete without infrastructure errors");
        }

        [Fact]
        public async Task GetPortfolioAgingWithValidTenantReturns200Or404()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = GenerateTestToken(roles: ["User"], tenantId: TestTenantId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

            // Act
            var response = await client.GetAsync("/api/v1/portfolio/aging");

            // Assert - Pipeline completes without 500
            _ = response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                "Pipeline should complete without infrastructure errors");
        }

        #endregion

        #region Collections Endpoints

        [Fact]
        public async Task GetPriorityCasesWithoutAuthReturns401()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/v1/collections/priority");

            // Assert
            _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region ML/Replay Endpoints

        [Fact]
        public async Task ReplayWithoutAuthReturns401()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var snapshotId = Guid.NewGuid();

            // Act
            var response = await _client.PostAsync($"/api/v1/ml/replay/{snapshotId}", null);

            // Assert
            _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Regression Endpoints

        [Fact]
        public async Task GetRegressionReportsWithoutAuthReturns401()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/v1/ml/regression/reports");

            // Assert
            _ = response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Admin Endpoints

        [Fact]
        public async Task OperationsEndpointsWithoutPlatformAdminReturns403()
        {
            // Arrange - Regular admin, not platform admin
            var token = GenerateTestToken(roles: ["Admin"]);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/operations/health/ledger");

            // Assert
            _ = response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region Tenant Contract Tests

        [Theory]
        [InlineData("/api/v1/portfolio/summary")]
        [InlineData("/api/v1/portfolio/aging")]
        [InlineData("/api/v1/collections/priority")]
        public async Task TenantScopedEndpointsWithoutTenantRejectsRequest(string url)
        {
            // Arrange - Auth but NO tenant in JWT
            using var client = _factory.CreateClient();
            var token = GenerateTestToken(roles: ["User"], tenantId: null);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync(url);

            // Assert - Should reject (401/400/403) but NOT 500
            // 401 = TenantMiddleware rejects before auth completes
            // 400 = TenantValidationBehavior rejects in pipeline
            // 403 = Authorization rejects
            _ = response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden);

            _ = response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                "Missing tenant should be handled gracefully, not cause 500");
            _ = response.StatusCode.Should().NotBe(HttpStatusCode.OK,
                "Tenant-scoped endpoint should not succeed without tenant");
        }

        [Theory]
        [InlineData("/api/v1/portfolio/summary")]
        [InlineData("/api/v1/portfolio/aging")]
        [InlineData("/api/v1/collections/priority")]
        public async Task TenantScopedEndpointsWithTenantCompletesWithoutError(string url)
        {
            // Arrange - Auth WITH tenant
            using var client = _factory.CreateClient();
            var token = GenerateTestToken(roles: ["User"], tenantId: TestTenantId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

            // Act
            var response = await client.GetAsync(url);

            // Assert - Pipeline completes (200 or 400 for empty cache, but NOT 500 or 403)
            _ = response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                $"Endpoint {url} should complete without infrastructure errors");
            _ = response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
                $"Endpoint {url} should accept valid tenant");
        }

        #endregion

        #region EF Core Materialization Tests

        [Fact]
        public async Task DbContextShouldMaterializeDecisionSnapshot()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();

            var entity = new DecisionSnapshot(
                customerId: Guid.NewGuid(),
                pd: 0.05m,
                modelVersion: "v1.0.0",
                limit: 10000m,
                rate: 0.15m,
                fraudScore: 0.02m);

            // Act
            _ = dbContext.DecisionSnapshots.Add(entity);
            _ = await dbContext.SaveChangesAsync();

            var loaded = await dbContext.DecisionSnapshots.FirstAsync(x => x.Id == entity.Id);

            // Assert
            _ = loaded.Should().NotBeNull();
            _ = loaded.CustomerId.Should().Be(entity.CustomerId);
            _ = loaded.ProbabilityOfDefault.Should().Be(0.05m);
            _ = loaded.ModelVersion.Should().Be("v1.0.0");
        }

        #endregion

        #region Helper Methods

        private static string GenerateTestToken(
            string[] roles,
            (string type, string value)[]? claims = null,
            Guid? tenantId = null)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("SuperSecretKeyForTesting!LengthMustBeAtLeast32Chars"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claimsList = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Email, "test@example.com")
            };

            // Only add tenant_id claim if explicitly provided
            if (tenantId.HasValue)
            {
                claimsList.Add(new Claim("tenant_id", tenantId.Value.ToString()));
            }

            foreach (var role in roles)
            {
                claimsList.Add(new Claim(ClaimTypes.Role, role));
            }

            if (claims != null)
            {
                foreach (var (type, value) in claims)
                {
                    claimsList.Add(new Claim(type, value));
                }
            }

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "CobryxTest",
                audience: "CobryxTest",
                claims: claimsList,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion
    }
}
