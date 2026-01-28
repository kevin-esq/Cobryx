using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;

namespace Cobryx.Application.Support.Commands.Create;

public record CreateSupportTicketCommand(
    string Title,
    string Description,
    SupportTicketPriority Priority,
    string? Category) : IRequest<Result<Guid>>;

public class CreateSupportTicketValidator : AbstractValidator<CreateSupportTicketCommand>
{
    public CreateSupportTicketValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
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
            return Result.Failure<Guid>("User or Tenant context missing.");
        }

        var ticket = new SupportTicket(
            tenantId.Value,
            userId.Value,
            request.Title,
            request.Description,
            request.Priority,
            request.Category);

        await _repository.AddAsync(ticket);

        return Result.Success(ticket.Id);
    }
}
