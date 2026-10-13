using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectM.Core.Entities;
using ProjectM.Infrastructure.Data;
using ProjectM.Services.DTOs;
using ProjectM.Services.Interfaces;

namespace ProjectM.Services.Services
{
    public class ClientCompanyService : IClientCompanyService
    {
        private readonly ApplicationDbContext _context;

        public ClientCompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ClientCompanyDto> GetClientCompanyByIdAsync(int id)
        {
            var company = await _context.ClientCompanies
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            return company == null ? null : MapToDto(company);
        }

        public async Task<ClientCompanyDto> GetClientCompanyByUserIdAsync(int userId)
        {
            var company = await _context.ClientCompanies
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return company == null ? null : MapToDto(company);
        }

        public async Task<IEnumerable<ClientCompanyDto>> GetAllClientCompaniesAsync()
        {
            var companies = await _context.ClientCompanies
                .Include(c => c.User)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            return companies.Select(MapToDto);
        }

        public async Task<IEnumerable<ClientCompanyDto>> GetClientCompaniesByStateAsync(string state)
        {
            var companies = await _context.ClientCompanies
                .Include(c => c.User)
                .Where(c => c.State == state)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            return companies.Select(MapToDto);
        }

        public async Task<ClientCompanyDto> CreateClientCompanyAsync(CreateClientCompanyDto createClientCompanyDto)
        {
            var company = new ClientCompany
            {
                UserId = createClientCompanyDto.UserId,
                CompanyName = createClientCompanyDto.CompanyName,
                Address = createClientCompanyDto.Address,
                City = createClientCompanyDto.City,
                State = createClientCompanyDto.State,
                Country = createClientCompanyDto.Country,
                PostalCode = createClientCompanyDto.PostalCode,
                TaxID = createClientCompanyDto.TRN,
                Industry = createClientCompanyDto.Industry,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClientCompanies.Add(company);
            await _context.SaveChangesAsync();

            return await GetClientCompanyByIdAsync(company.Id);
        }

        public async Task<ClientCompanyDto> UpdateClientCompanyAsync(int id, UpdateClientCompanyDto updateClientCompanyDto)
        {
            var company = await _context.ClientCompanies.FindAsync(id);
            if (company == null)
                return null;

            if (!string.IsNullOrEmpty(updateClientCompanyDto.CompanyName))
                company.CompanyName = updateClientCompanyDto.CompanyName;
            if (updateClientCompanyDto.Address != null)
                company.Address = updateClientCompanyDto.Address;
            if (updateClientCompanyDto.City != null)
                company.City = updateClientCompanyDto.City;
            if (updateClientCompanyDto.State != null)
                company.State = updateClientCompanyDto.State;
            if (updateClientCompanyDto.Country != null)
                company.Country = updateClientCompanyDto.Country;
            if (updateClientCompanyDto.PostalCode != null)
                company.PostalCode = updateClientCompanyDto.PostalCode;
            if (updateClientCompanyDto.TRN != null)
                company.TaxID = updateClientCompanyDto.TRN;
            if (updateClientCompanyDto.Industry != null)
                company.Industry = updateClientCompanyDto.Industry;

            company.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetClientCompanyByIdAsync(id);
        }

        public async Task<bool> DeleteClientCompanyAsync(int id)
        {
            var company = await _context.ClientCompanies.FindAsync(id);
            if (company == null)
                return false;

            _context.ClientCompanies.Remove(company);
            await _context.SaveChangesAsync();
            return true;
        }

        private ClientCompanyDto MapToDto(ClientCompany company)
        {
            return new ClientCompanyDto
            {
                Id = company.Id,
                UserId = company.UserId,
                UserName = company.User?.Name ?? "",
                CompanyName = company.CompanyName,
                Address = company.Address ?? "",
                City = company.City ?? "",
                State = company.State ?? "",
                Country = company.Country ?? "",
                PostalCode = company.PostalCode ?? "",
                TRN = company.TaxID ?? "",
                Industry = company.Industry ?? "",
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt
            };
        }
    }
}