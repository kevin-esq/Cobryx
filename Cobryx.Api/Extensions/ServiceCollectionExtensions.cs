using Asp.Versioning;

using Cobryx.Api.Filters;
using Cobryx.Api.Filters.Observability;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Shared;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

using OpenTelemetry.Metrics;

namespace Cobryx.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCobryxOpenTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<CobryxMetrics>();
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(CobryxMetrics.MeterName);
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddPrometheusExporter();
            });

        return services;
    }

    public static IServiceCollection AddCobryxSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Cobryx API",
                Version = "v1",
                Description =
                    "Cobryx Financial Platform API. Optimized for scalability with flat resource paths and dedicated system command namespaces."
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"{token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            c.CustomSchemaIds(SwaggerSchemaHelper.GetSchemaId);

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            const string appXmlFile = "Cobryx.Application.xml";
            var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFile);
            if (File.Exists(appXmlPath))
            {
                c.IncludeXmlComments(appXmlPath, includeControllerXmlComments: true);
            }

            c.EnableAnnotations();
        });

        return services;
    }

    public static IServiceCollection AddCobryxControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                options.Filters.Add<SessionValidationFilter>();
                options.Filters.Add<ObservabilityFilter>();
                options.Filters.Add<IdempotencyKeyFilter>();
                options.Filters.Add<PlanGatingFilter>();
            })
            .AddJsonOptions(json =>
            {
                json.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

        return services;
    }

    public static IServiceCollection AddCobryxRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 5;
                opt.QueueLimit = 0;
            });
        });

        return services;
    }

    public static IServiceCollection AddCobryxAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IApiLinkGenerator, ApiLinkGenerator>();

        services.AddAuthorizationBuilder()
            .AddPolicy("CanViewCustomers",
                policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "customers:view"))
            .AddPolicy("CanCreateCustomers",
                policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "customers:create"))
            .AddPolicy("CanViewCredits", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "credits:view"))
            .AddPolicy("CanCreateCredits",
                policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "credits:create"))
            .AddPolicy("CanApplyPayments",
                policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "payments:apply"))
            .AddPolicy("CanManageTenant", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "tenant:manage"))
            .AddPolicy("PlatformAdmin", policy => policy.RequireClaim(CobryxClaimTypes.Permissions, "platform:admin"))
            .AddPolicy("EmailVerified", policy => policy.RequireClaim(CobryxClaimTypes.EmailVerified, "true"))
            .AddPolicy("AccountVerified", policy =>
                policy.RequireClaim(CobryxClaimTypes.EmailVerified, "true")
                    .RequireClaim(CobryxClaimTypes.RequiresOnboarding, "false"));

        return services;
    }

    public static IServiceCollection AddCobryxApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("X-Api-Version"),
                new UrlSegmentApiVersionReader());
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IServiceCollection AddCobryxCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCors", policy =>
            {
                var appOptions = configuration.GetSection("App").Get<AppOptions>() ?? new AppOptions();
                policy.WithOrigins(appOptions.AppUrl)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}

internal static class SwaggerSchemaHelper
{
    public static string GetSchemaId(Type type)
    {
        if (!type.IsGenericType)
        {
            if (type.Namespace != null && type.Namespace.StartsWith("Cobryx.", StringComparison.Ordinal))
            {
                var suffix = type.Namespace
                    .Replace("Cobryx.", "")
                    .Replace(".", "_");
                return $"{suffix}_{type.Name}";
            }

            return type.Name;
        }

        var genericName = type.Name.Split('`')[0];
        var genericArgs = string.Join("Of", type.GetGenericArguments().Select(GetSchemaId));
        return $"{genericName}{genericArgs}";
    }
}
