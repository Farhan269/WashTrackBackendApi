using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.WashService;


namespace wsahRecieveDelivary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class WashDhuController : ControllerBase
    {
        private readonly WashDhuService _service;

        public WashDhuController(WashDhuService service)
        {
            _service = service;
        }


        [HttpGet("GetDryProcessSummary")]
        public async Task<IActionResult> GetDryProcessSummary(
     [FromQuery] DryProcessSummaryFilterDto filter)
        {
            var result = await _service
                .GetDryProcessSummaryAsync(filter);

            return Ok(result);
        }
        
        [HttpGet("GetTopIssues")]
        public async Task<IActionResult> GetTopIssues([FromQuery] DryProcessSummaryFilterDto filter)
        {
            var result = await _service.GetTopIssuesAsync(filter);
            return Ok(result);
        }

        [HttpGet("GetWetProcessSummary")]
        public async Task<IActionResult> GetWetProcessSummary(
     [FromQuery] DryProcessSummaryFilterDto filter)
        {
            var result = await _service
                .GetWetProcessSummaryAsync(filter);

            return Ok(result);
        }

        [HttpGet("GetWetTopIssues")]
        public async Task<IActionResult> GetWetTopIssues([FromQuery] DryProcessSummaryFilterDto filter)
        {
            var result = await _service.GetWetTopIssuesAsync(filter);
            return Ok(result);
        }
    }
}
