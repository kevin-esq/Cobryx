using System.Diagnostics;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

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
            var response = await next(cancellationToken);
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

            return await next(cancellationToken);
        }
    }

    public class Audit<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return next(cancellationToken);
        }
    }

    public class UnitOfWork<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly Cobryx.Domain.Interfaces.IUnitOfWork _unitOfWork;

        public UnitOfWork(Cobryx.Domain.Interfaces.IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next(cancellationToken);

            if (ShouldSave(response))
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return response;
        }

        private static bool ShouldSave(TResponse response)
        {
            if (response is null) return false;

            if (response is Result result)
            {
                return result.IsSuccess;
            }

            var type = response.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var isSuccessProp = type.GetProperty("IsSuccess");
                if (isSuccessProp != null && isSuccessProp.GetValue(response) is bool isSuccess)
                {
                    return isSuccess;
                }
            }

            return true;
        }
    }
}
