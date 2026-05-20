using Asp.Versioning;

using Cobryx.Application.Dashboard.Common;
using Cobryx.Application.Dashboard.Queries.GetGuidedSetup;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/guided-setup")]
public class GuidedSetupController(ISender sender) : CobryxBaseController(sender)
{

    /// <summary>
    /// Retrieves the 'Next Best Action' for the current tenant to drive onboarding.
    /// Provides semantic codes and data context for the frontend to render guided steps.
    /// </summary>
    [HttpGet("next-action")]
    [ProducesResponseType(typeof(ApiSuccessResponse<NextBestActionDto>), 200)]
    public async Task<IActionResult> GetNextAction()
    {
        Result<NextBestActionDto> result = await Sender.Send(new GetGuidedSetupQuery());
        return HandleResult(result);
    }
}
