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
    public class SLAsController : ControllerBase
    {
        private readonly ISLAService _slaService;

        public SLAsController(ISLAService slaService)
        {
            _slaService = slaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSLAs()
        {
            var slas = await _slaService.GetAllSLAsAsync();
            return Ok(slas);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSLA(int id)
        {
            var sla = await _slaService.GetSLAByIdAsync(id);
            if (sla == null)
                return NotFound();

            return Ok(sla);
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetSLAsByClient(int clientId)
        {
            var slas = await _slaService.GetSLAsByClientAsync(clientId);
            return Ok(slas);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetSLAsByStatus(string status)
        {
            var slas = await _slaService.GetSLAsByStatusAsync(status);
            return Ok(slas);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateSLA([FromBody] CreateSLADto createSLADto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var sla = await _slaService.CreateSLAAsync(createSLADto, userId);
            return CreatedAtAction(nameof(GetSLA), new { id = sla.Id }, sla);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateSLA(int id, [FromBody] UpdateSLADto updateSLADto)
        {
            var sla = await _slaService.UpdateSLAAsync(id, updateSLADto);
            if (sla == null)
                return NotFound();

            return Ok(sla);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSLA(int id)
        {
            var result = await _slaService.DeleteSLAAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ActivateSLA(int id)
        {
            var result = await _slaService.ActivateSLAAsync(id);
            if (!result)
                return NotFound();

            return Ok(new { message = "SLA activated successfully" });
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeactivateSLA(int id)
        {
            var result = await _slaService.DeactivateSLAAsync(id);
            if (!result)
                return NotFound();

            return Ok(new { message = "SLA deactivated successfully" });
        }
    }
}
