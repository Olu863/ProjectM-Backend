using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectM.API.Authorization;
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
            // Clients only ever see their own repairs; staff see everything.
            var repairs = User.IsStaff()
                ? await _repairService.GetAllRepairsAsync()
                : await _repairService.GetRepairsByClientAsync(User.GetUserId());
            return Ok(repairs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRepair(int id)
        {
            var repair = await _repairService.GetRepairByIdAsync(id);
            if (repair == null || (!User.IsStaff() && repair.ClientId != User.GetUserId()))
                return NotFound();

            return Ok(repair);
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetRepairsByClient(int clientId)
        {
            if (!User.IsStaff() && clientId != User.GetUserId())
                return Forbid();

            var repairs = await _repairService.GetRepairsByClientAsync(clientId);
            return Ok(repairs);
        }

        [HttpGet("technician/{technicianId}")]
        [Authorize(Roles = AppRoles.Staff)]
        public async Task<IActionResult> GetRepairsByTechnician(int technicianId)
        {
            var repairs = await _repairService.GetRepairsByTechnicianAsync(technicianId);
            return Ok(repairs);
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = AppRoles.Staff)]
        public async Task<IActionResult> GetRepairsByStatus(string status)
        {
            var repairs = await _repairService.GetRepairsByStatusAsync(status);
            return Ok(repairs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRepair([FromBody] CreateRepairDto createRepairDto)
        {
            var userId = User.GetUserId();
            if (userId == 0)
                return Unauthorized();

            // A client can only log repairs against their own account; staff choose the client.
            if (!User.IsStaff())
                createRepairDto.ClientId = userId;

            var repair = await _repairService.CreateRepairAsync(createRepairDto, userId);
            if (repair == null)
                return BadRequest(new { message = "Unable to create repair." });

            return CreatedAtAction(nameof(GetRepair), new { id = repair.Id }, repair);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Staff)]
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
        [Authorize(Roles = AppRoles.Staff)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var result = await _repairService.UpdateStatusAsync(id, status);
            if (!result)
                return NotFound();

            return Ok(new { message = "Status updated successfully" });
        }

        [HttpPost("{id}/complete")]
        [Authorize(Roles = AppRoles.Staff)]
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
