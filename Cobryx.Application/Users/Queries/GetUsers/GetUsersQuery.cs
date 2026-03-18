using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Application.Users.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Users.Queries.GetUsers;

public record GetUsersQuery(int Page = 1, int PageSize = 10) : IRequest<Result<PaginatedList<UserDto>>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, Result<PaginatedList<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetUsersHandler(IUserRepository userRepository, ITenantProvider tenantProvider)
    {
        _userRepository = userRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<PaginatedList<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<PaginatedList<UserDto>>(DomainErrorCode.Tenant.ContextMissing);

        var users = await _userRepository.GetByTenantAsync(tenantId.Value, cancellationToken);

        var totalCount = users.Count();
        var items = users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.Email,
                u.Role?.Name ?? Role.Constants.User,
                u.CreatedAt))
            .ToList();

        return Result.Success(new PaginatedList<UserDto>(
            items,
            totalCount,
            request.Page,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }
}
