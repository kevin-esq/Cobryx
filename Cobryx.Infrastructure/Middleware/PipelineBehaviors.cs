using System.Diagnostics;
using Cobryx.Application.Common.Interfaces;
using Concordia;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Middleware;

public static class PipelineBehaviors
{
    public class Logging<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<Logging<TRequest, TResponse>> _logger;
        private readonly ITenantProvider _tenantProvider;

        public Logging(ILogger<Logging<TRequest, TResponse>> logger, ITenantProvider tenantProvider)
        {
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var tenantId = _tenantProvider.GetTenantId();
            
            _logger.LogInformation("Cobryx Request: {Name} {@TenantId} {@Request}", requestName, tenantId, request);

            var stopwatch = Stopwatch.StartNew();
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation("Cobryx Response: {Name} Processed in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
    }

    public class Validation<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public Validation(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                if (failures.Count != 0)
                    throw new ValidationException(failures);
            }

            return await next();
        }
    }

    public class Audit<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return next();
        }
    }
}
