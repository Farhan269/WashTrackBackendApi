using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.IRepository;
using wsahRecieveDelivary.Repository;
using wsahRecieveDelivary.WashService;


namespace wsahRecieveDelivary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TusukaExtremeController : ControllerBase
    {
        private readonly ITusukaExtremeRepository _repo;

        public TusukaExtremeController(ITusukaExtremeRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("get-wash-delivery")]
        public async Task<IActionResult> GetWashDelivery(
    [FromQuery] DateOnly? fromDate,
    [FromQuery] DateOnly? toDate,
    [FromQuery] List<string>? plant,
    [FromQuery] List<string>? washUnit)
        {
            var result = await _repo.GetWashDeliveryAsync(
                fromDate,
                toDate,
                plant,
                washUnit);

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [HttpGet("get-wash-delivery-details")]
        public async Task<IActionResult> GetWashDeliveryDetails(
    [FromQuery] DateOnly? fromDate,
    [FromQuery] DateOnly? toDate,
    [FromQuery] List<string>? plant,
    [FromQuery] List<string>? washUnit,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20)
        {
            var result = await _repo.GetWashDeliveryDetailsAsync(
                fromDate,
                toDate,
                plant,
                washUnit,
                pageNumber,
                pageSize);

            return Ok(result);
        }
    }
}
