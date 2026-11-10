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
    public class PFIsController : ControllerBase
    {
        private readonly IPFIService _pfiService;
        private readonly IRepairService _repairService;

        public PFIsController(IPFIService pfiService, IRepairService repairService)
        {
            _pfiService = pfiService;
            _repairService = repairService;
        }

        // Staff can see every PFI; a client only the PFIs that belong to their own repairs.
        private async Task<bool> CanAccessRepairAsync(int repairId)
        {
            if (User.IsStaff())
                return true;

            var repair = await _repairService.GetRepairByIdAsync(repairId);
            return repair != null && repair.ClientId == User.GetUserId();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllPFIs()
        {
            var pfis = await _pfiService.GetAllPFIsAsync();
            return Ok(pfis);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPFI(int id)
        {
            var pfi = await _pfiService.GetPFIByIdAsync(id);
            if (pfi == null || !await CanAccessRepairAsync(pfi.RepairId))
                return NotFound();

            return Ok(pfi);
        }

        [HttpGet("repair/{repairId}")]
        public async Task<IActionResult> GetPFIsByRepair(int repairId)
        {
            if (!await CanAccessRepairAsync(repairId))
                return NotFound();

            var pfis = await _pfiService.GetPFIsByRepairAsync(repairId);
            return Ok(pfis);
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetPFIsByStatus(string status)
        {
            var pfis = await _pfiService.GetPFIsByStatusAsync(status);
            return Ok(pfis);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Technician")]
        public async Task<IActionResult> CreatePFI([FromBody] CreatePFIDto createPFIDto)
        {
            var pfi = await _pfiService.CreatePFIAsync(createPFIDto);
            if (pfi == null)
                return BadRequest(new { message = "Unable to create PFI." });

            return CreatedAtAction(nameof(GetPFI), new { id = pfi.Id }, pfi);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdatePFI(int id, [FromBody] UpdatePFIDto updatePFIDto)
        {
            var pfi = await _pfiService.UpdatePFIAsync(id, updatePFIDto);
            if (pfi == null)
                return NotFound();

            return Ok(pfi);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ApprovePFI(int id)
        {
            var userId = User.GetUserId();
            var result = await _pfiService.ApprovePFIAsync(id, userId);
            if (!result)
                return NotFound();

            return Ok(new { message = "PFI approved successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePFI(int id)
        {
            var result = await _pfiService.DeletePFIAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
