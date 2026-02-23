using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Users.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Users.Queries.GetTenantUsers;

public record GetTenantUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedList<UserListDto>>;

public class GetTenantUsersHandler : IRequestHandler<GetTenantUsersQuery, PagedList<UserListDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetTenantUsersHandler(IUserRepository userRepository, ITenantProvider tenantProvider)
    {
        _userRepository = userRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<PagedList<UserListDto>> Handle(GetTenantUsersQuery request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? throw new DomainException(DomainErrorCode.Tenant.ContextMissing);

        var (users, totalCount) = await _userRepository.GetByTenantPagedAsync(
            tenantId,
            request.Page,
            request.PageSize,
            ct);

        var dtos = users.Select(u => new UserListDto(
            u.Id,
            u.FullName,
            u.Email.Value,
            u.Role?.Name ?? "Unknown",
            u.IsActive,
            u.CreatedAt));

        return new PagedList<UserListDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
