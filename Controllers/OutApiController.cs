using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class OutApiController : ControllerBase
    {
        private readonly IOutServiceApi _outapiService;

        public OutApiController(IOutServiceApi outapiService)
        {
            _outapiService = outapiService;
        }

        [HttpGet("dry-process-summary")]
        public async Task<IActionResult> GetDryProcessSummary(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
        {
            var result = await _outapiService.GetDryProcessSummaryAsync(
                fromDate,
                toDate);

            return Ok(result);
        }
    }
}
