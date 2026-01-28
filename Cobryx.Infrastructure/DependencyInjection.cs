using Cobryx.Infrastructure.Persistence;
using Cobryx.Infrastructure.Persistence.Interceptors;
using Cobryx.Infrastructure.Repositories;
using Cobryx.Infrastructure.MultiTenancy;
using Cobryx.Infrastructure.Middleware;
using Cobryx.Domain.Interfaces;
using Cobryx.Application.Common.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Cobryx.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        services.AddScoped<IDomainEventService, DomainEventService>();

        // Interceptors
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();

        // Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Logging<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Validation<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineBehaviors.Audit<,>));

        // EF Core Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            KeepAlive = 30,
            CommandTimeout = 300,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 20
        };

        // IPv4 resolution for Docker compatibility
        try
        {
            if (!string.IsNullOrEmpty(npgsqlBuilder.Host))
            {
                var ips = System.Net.Dns.GetHostAddresses(npgsqlBuilder.Host);
                var ipv4 = ips.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (ipv4 != null)
                {
                    npgsqlBuilder.Host = ipv4.ToString();
                }
            }
        }
        catch { }

        services.AddDbContext<CobryxDbContext>((sp, options) =>
        {
            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<DispatchDomainEventsInterceptor>());

            options.UseNpgsql(npgsqlBuilder.ToString(), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        });

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICreditRepository, CreditRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ITaxConfigurationRepository, TaxConfigurationRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<Cobryx.Domain.Services.PaymentService>();
        services.AddScoped<Cobryx.Domain.Services.UsageService>();

        // Identity Services
        services.AddScoped<IPasswordHasher, Identity.PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, Identity.JwtTokenGenerator>();
        services.AddScoped<IInvoiceNumberService, Services.InvoiceNumberService>();

        // Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });

        return services;
    }
}
