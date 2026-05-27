using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Validation;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Support.Commands.Create;

[TenantScoped]
public record CreateSupportTicketCommand(
    string Title,
    string Description,
    SupportTicketPriority Priority,
    SupportTicketCategory Category) : IRequest<Result<Guid>>, IRequiresTenant;

public class CreateSupportTicketValidator : AbstractValidator<CreateSupportTicketCommand>
{
    public CreateSupportTicketValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithErrorCode(SupportValidationErrors.Subject.Required)
            .MaximumLength(200).WithErrorCode(SupportValidationErrors.Subject.TooLong);
        RuleFor(x => x.Description)
            .NotEmpty().WithErrorCode(SupportValidationErrors.Description.Required);
    }
}

public class CreateSupportTicketHandler : IRequestHandler<CreateSupportTicketCommand, Result<Guid>>
{
    private readonly ISupportTicketRepository _repository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _currentUserProvider;

    public CreateSupportTicketHandler(
        ISupportTicketRepository repository,
        ITenantProvider tenantProvider,
        ICurrentUserProvider currentUserProvider)
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<Guid>> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = _currentUserProvider.GetUserId();

        if (tenantId == null || userId == null)
        {
            return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);
        }

        var ticket = new SupportTicket(
            tenantId.Value,
            userId.Value,
            request.Title,
            request.Description,
            request.Priority,
            request.Category);

        await _repository.AddAsync(ticket, cancellationToken);

        return Result.Success(ticket.Id);
    }
}
