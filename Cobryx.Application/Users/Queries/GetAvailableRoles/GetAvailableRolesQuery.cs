using Cobryx.Application.Users.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Users.Queries.GetAvailableRoles
{
    public record GetAvailableRolesQuery : IRequest<Result<IEnumerable<RoleDto>>>;

    public class GetAvailableRolesHandler(
        IRoleRepository roleRepository) : IRequestHandler<GetAvailableRolesQuery, Result<IEnumerable<RoleDto>>>
    {
        public async Task<Result<IEnumerable<RoleDto>>> Handle(GetAvailableRolesQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<Role> roles = await roleRepository.GetAllAsync(cancellationToken);

            IEnumerable<RoleDto> result = roles
                .Where(r => r.Name != Role.Constants.Owner)
                .Select(r => new RoleDto(r.Id, r.Name, r.Description))
                .OrderBy(r => r.Name);

            return Result.Success(result);
        }
    }
}
