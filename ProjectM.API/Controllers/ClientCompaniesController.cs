using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectM.Services.DTOs;
using ProjectM.Services.Interfaces;

namespace ProjectM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientCompaniesController : ControllerBase
    {
        private readonly IClientCompanyService _clientCompanyService;

        public ClientCompaniesController(IClientCompanyService clientCompanyService)
        {
            _clientCompanyService = clientCompanyService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllClientCompanies()
        {
            var companies = await _clientCompanyService.GetAllClientCompaniesAsync();
            return Ok(companies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientCompany(int id)
        {
            var company = await _clientCompanyService.GetClientCompanyByIdAsync(id);
            if (company == null)
                return NotFound();

            return Ok(company);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetClientCompanyByUser(int userId)
        {
            var company = await _clientCompanyService.GetClientCompanyByUserIdAsync(userId);
            if (company == null)
                return NotFound();

            return Ok(company);
        }

        [HttpGet("state/{state}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetClientCompaniesByState(string state)
        {
            var companies = await _clientCompanyService.GetClientCompaniesByStateAsync(state);
            return Ok(companies);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateClientCompany([FromBody] CreateClientCompanyDto createClientCompanyDto)
        {
            var company = await _clientCompanyService.CreateClientCompanyAsync(createClientCompanyDto);
            return CreatedAtAction(nameof(GetClientCompany), new { id = company.Id }, company);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClientCompany(int id, [FromBody] UpdateClientCompanyDto updateClientCompanyDto)
        {
            var company = await _clientCompanyService.UpdateClientCompanyAsync(id, updateClientCompanyDto);
            if (company == null)
                return NotFound();

            return Ok(company);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteClientCompany(int id)
        {
            var result = await _clientCompanyService.DeleteClientCompanyAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
