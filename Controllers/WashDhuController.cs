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
        [AllowAnonymous]
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


        [HttpGet("GetDryProcessDetails")]
        public async Task<IActionResult> GetDryProcessDetails(
     [FromQuery] DryProcessDetailsFilterDto filter)
        {
            var result = await _service
                .GetDryProcessDetailsAsync(filter);

            return Ok(result);
        }


        [HttpGet("GetWetProcessDetails")]
        public async Task<IActionResult> GetWetProcessDetails(
     [FromQuery] DryProcessDetailsFilterDto filter)
        {
            var result = await _service
                .GetWetProcessDetailsAsync(filter);

            return Ok(result);
        }

        [HttpGet("GetDryProcessHourlyDetails")]
        public async Task<IActionResult> GetDryProcessHourlyDetailsAsync(
    [FromQuery] DhuDryProcessHourlyDetailsFilterDto filter)
        {
            var result = await _service
                .GetDryProcessHourlyDetailsAsync(filter);

            return Ok(result);
        }

        [HttpGet("GetWetProcessHourlyDetails")]
        public async Task<IActionResult> GetWetProcessHourlyDetailsAsync(
    [FromQuery] DhuDryProcessHourlyDetailsFilterDto filter)
        {
            var result = await _service
                .GetWetProcessHourlyDetailsAsync(filter);

            return Ok(result);
        }

    }
}
