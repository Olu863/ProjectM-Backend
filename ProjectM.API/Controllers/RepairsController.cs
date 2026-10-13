using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectM.Services.DTOs;
using ProjectM.Services.Interfaces;
using System.Security.Claims;

namespace ProjectM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RepairsController : ControllerBase
    {
        private readonly IRepairService _repairService;

        public RepairsController(IRepairService repairService)
        {
            _repairService = repairService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRepairs()
        {
            var repairs = await _repairService.GetAllRepairsAsync();
            return Ok(repairs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRepair(int id)
        {
            var repair = await _repairService.GetRepairByIdAsync(id);
            if (repair == null)
                return NotFound();

            return Ok(repair);
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetRepairsByClient(int clientId)
        {
            var repairs = await _repairService.GetRepairsByClientAsync(clientId);
            return Ok(repairs);
        }

        [HttpGet("technician/{technicianId}")]
        public async Task<IActionResult> GetRepairsByTechnician(int technicianId)
        {
            var repairs = await _repairService.GetRepairsByTechnicianAsync(technicianId);
            return Ok(repairs);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetRepairsByStatus(string status)
        {
            var repairs = await _repairService.GetRepairsByStatusAsync(status);
            return Ok(repairs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRepair([FromBody] CreateRepairDto createRepairDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var repair = await _repairService.CreateRepairAsync(createRepairDto, userId);
            return CreatedAtAction(nameof(GetRepair), new { id = repair.Id }, repair);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRepair(int id, [FromBody] UpdateRepairDto updateRepairDto)
        {
            var repair = await _repairService.UpdateRepairAsync(id, updateRepairDto);
            if (repair == null)
                return NotFound();

            return Ok(repair);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteRepair(int id)
        {
            var result = await _repairService.DeleteRepairAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/assign-technician/{technicianId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignTechnician(int id, int technicianId)
        {
            var result = await _repairService.AssignTechnicianAsync(id, technicianId);
            if (!result)
                return NotFound();

            return Ok(new { message = "Technician assigned successfully" });
        }

        [HttpPost("{id}/update-status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var result = await _repairService.UpdateStatusAsync(id, status);
            if (!result)
                return NotFound();

            return Ok(new { message = "Status updated successfully" });
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteRepair(int id, [FromBody] CompleteRepairRequest request)
        {
            var result = await _repairService.CompleteRepairAsync(id, request.ActualCost, request.ActualDowntime);
            if (!result)
                return NotFound();

            return Ok(new { message = "Repair completed successfully" });
        }
    }

    public class CompleteRepairRequest
    {
        public decimal ActualCost { get; set; }
        public int ActualDowntime { get; set; }
    }
}
