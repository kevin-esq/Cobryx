using System.Diagnostics;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Middleware
{
    public static class PipelineBehaviors
    {
        public class Logging<TRequest, TResponse>(
            ILogger<Logging<TRequest, TResponse>> logger,
            ITenantProvider tenantProvider) : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
                CancellationToken cancellationToken)
            {
                var requestName = typeof(TRequest).Name;
                var tenantId = tenantProvider.GetTenantId();

                logger.LogInformation("Cobryx Request: {Name} {@TenantId} {@Request}", requestName, tenantId, request);

                var stopwatch = Stopwatch.StartNew();
                var response = await next(cancellationToken);
                stopwatch.Stop();

                logger.LogInformation("Cobryx Response: {Name} Processed in {ElapsedMilliseconds}ms", requestName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
        }

        public class Validation<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
            : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
                CancellationToken cancellationToken)
            {
                if (_validators.Any())
                {
                    var context = new ValidationContext<TRequest>(request);
                    var validationResults =
                        await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                    var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

                    if (failures.Count != 0)
                    {
                        throw new ValidationException(failures);
                    }
                }

                return await next(cancellationToken);
            }
        }

        public class Audit<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
                CancellationToken cancellationToken) => next(cancellationToken);
        }

        public class UnitOfWork<TRequest, TResponse>(Domain.Interfaces.IUnitOfWork unitOfWork)
            : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            private readonly Domain.Interfaces.IUnitOfWork _unitOfWork = unitOfWork;

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
                CancellationToken cancellationToken)
            {
                var response = await next(cancellationToken);

                if (ShouldSave(response))
                {
                    _ = await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                return response;
            }

            private static bool ShouldSave(TResponse response)
            {
                if (response is null)
                {
                    return false;
                }

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

        /// <summary>
        /// Pipeline behavior that validates tenant context is present for requests that require it.
        /// Requests can opt-in by implementing <see cref="IRequiresTenant"/>.
        /// </summary>
        public class TenantValidation<TRequest, TResponse>(
            ITenantProvider tenantProvider,
            ILogger<TenantValidation<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            private readonly ITenantProvider _tenantProvider = tenantProvider;
            private readonly ILogger<TenantValidation<TRequest, TResponse>> _logger = logger;

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
                CancellationToken cancellationToken)
            {
                if (request is IRequiresTenant)
                {
                    var tenantId = _tenantProvider.GetTenantId();
                    if (tenantId == null || tenantId == Guid.Empty)
                    {
                        _logger.LogWarning("Tenant context missing for request {RequestType}", typeof(TRequest).Name);
                        return CreateFailureResult();
                    }
                }

                return await next(cancellationToken);
            }

            private static TResponse CreateFailureResult()
            {
                var responseType = typeof(TResponse);

                if (responseType == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(DomainErrorCode.Tenant.ContextMissing);
                }

                if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var innerType = responseType.GetGenericArguments()[0];
                    var failureMethod = typeof(Result).GetMethod("Failure", 1, [typeof(DomainErrorCode)])!
                        .MakeGenericMethod(innerType);
                    return (TResponse)failureMethod.Invoke(null, [DomainErrorCode.Tenant.ContextMissing])!;
                }

                throw new InvalidOperationException($"Cannot create failure result for type {responseType}");
            }
        }
    }
}
