using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Users.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Users.Queries.GetTenantUsers
{
    [TenantScoped]
public record GetTenantUsersQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedList<UserListDto>>>, IRequiresTenant;

    public class GetTenantUsersHandler(
        IUserRepository userRepository,
        ITenantProvider tenantProvider) : IRequestHandler<GetTenantUsersQuery, Result<PagedList<UserListDto>>>
    {
        public async Task<Result<PagedList<UserListDto>>> Handle(GetTenantUsersQuery request,
            CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<PagedList<UserListDto>>(DomainErrorCode.Tenant.ContextMissing);
            }

            (IEnumerable<User> users, var totalCount) = await userRepository.GetByTenantPagedAsync(
                tenantId.Value,
                request.Page,
                request.PageSize,
                cancellationToken);

            IEnumerable<UserListDto> dtos = users.Select(u => new UserListDto(
                u.Id,
                u.FullName,
                u.Email.Value,
                u.Role.Name,
                u.IsActive,
                u.CreatedAt));

            return Result.Success(new PagedList<UserListDto>(dtos, totalCount, request.Page, request.PageSize));
        }
    }
}
