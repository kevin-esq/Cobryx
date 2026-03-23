using Cobryx.Application.Users.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;

using Concordia;

namespace Cobryx.Application.Users.Queries.GetAvailableRoles;

public record GetAvailableRolesQuery() : IRequest<IEnumerable<RoleDto>>;

public class GetAvailableRolesHandler : IRequestHandler<GetAvailableRolesQuery, IEnumerable<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetAvailableRolesHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IEnumerable<RoleDto>> Handle(GetAvailableRolesQuery request, CancellationToken ct)
    {
        var roles = await _roleRepository.GetAllAsync(ct);

        return roles
            .Where(r => r.Name != Role.Constants.Owner)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description))
            .OrderBy(r => r.Name);
    }
}
