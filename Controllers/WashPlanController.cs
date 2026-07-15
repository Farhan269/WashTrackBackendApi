using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class WashPlanController : ControllerBase
    {
        private readonly IWashPlan _washplanService;

        public WashPlanController(IWashPlan washplanService)
        {
            _washplanService = washplanService;
        }


        //[HttpGet("machines")]
        //public async Task<IActionResult> GetMachineList(int? plantId = null, int? unitId = null)
        //{
        //    var result = await _washplanService.GetMachineListAsync(plantId,unitId);
        //    return Ok(result);
        //}


        [HttpPost]
        [Route("CreateWashPlan")]

        public async Task<MessageHelper> CreateWashPlanAsync(List<CreateWashPlanDto> data)
        {
            try
            {
                var msg = await _washplanService.CreateWashPlanAsync(data);
                return msg;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpDelete]
        [Route("DeleteWashPlan")]

        public async Task<MessageHelper> DeleteWashPlanAsync(long washPlanId, int? UpdatedBy)
        {
            try
            {
                var msg = await _washplanService.DeleteWashPlanAsync(washPlanId, UpdatedBy);
                return msg;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //[HttpGet]
        //[Route("PlantUnitList")]
        //public async Task<IActionResult> PlantUnitListAsync()
        //{
        //    try
        //    {
        //        var result = await _washplanService.GetPlantUnitListAsync();
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        [HttpGet("get-wash-plan")]
        public async Task<IActionResult> GetWashPlan([FromQuery] WashPlanFilterDto filter)
        {
            var result = await _washplanService.GetWashPlanAsync(filter);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpGet("get-wash-plan-modal")]
        public async Task<IActionResult> GetWashPlanModal([FromQuery] WashPlanModalFilterDto filter)
        {
            var result = await _washplanService.GetWashPlanModalAsync(filter);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
