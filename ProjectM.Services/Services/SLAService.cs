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
    public class SLAService : ISLAService
    {
        private readonly ApplicationDbContext _context;

        public SLAService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SLADto> GetSLAByIdAsync(int id)
        {
            var sla = await _context.SLAs
                .Include(s => s.Client)
                .Include(s => s.Creator)
                .FirstOrDefaultAsync(s => s.Id == id);

            return sla == null ? null : MapToDto(sla);
        }

        public async Task<IEnumerable<SLADto>> GetAllSLAsAsync()
        {
            var slas = await _context.SLAs
                .Include(s => s.Client)
                .Include(s => s.Creator)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return slas.Select(MapToDto);
        }

        public async Task<IEnumerable<SLADto>> GetSLAsByClientAsync(int clientId)
        {
            var slas = await _context.SLAs
                .Include(s => s.Client)
                .Include(s => s.Creator)
                .Where(s => s.ClientId == clientId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return slas.Select(MapToDto);
        }

        public async Task<IEnumerable<SLADto>> GetSLAsByStatusAsync(string status)
        {
            var slas = await _context.SLAs
                .Include(s => s.Client)
                .Include(s => s.Creator)
                .Where(s => s.Status == status)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return slas.Select(MapToDto);
        }

        public async Task<SLADto> CreateSLAAsync(CreateSLADto createSLADto, int createdBy)
        {
            var sla = new SLA
            {
                ClientId = createSLADto.ClientId,
                Title = createSLADto.SLAType,
                ServiceScope = createSLADto.Terms,
                ContractValue = createSLADto.ContractValue,
                RenewalDate = createSLADto.RenewalDate ?? DateTime.UtcNow.AddYears(1),
                Status = "Active",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.SLAs.Add(sla);
            await _context.SaveChangesAsync();

            return await GetSLAByIdAsync(sla.Id);
        }

        public async Task<SLADto> UpdateSLAAsync(int id, UpdateSLADto updateSLADto)
        {
            var sla = await _context.SLAs.FindAsync(id);
            if (sla == null)
                return null;

            if (!string.IsNullOrEmpty(updateSLADto.SLAType))
                sla.Title = updateSLADto.SLAType;
            if (updateSLADto.ContractValue.HasValue)
                sla.ContractValue = updateSLADto.ContractValue;
            if (updateSLADto.RenewalDate.HasValue)
                sla.RenewalDate = updateSLADto.RenewalDate.Value;
            if (!string.IsNullOrEmpty(updateSLADto.Status))
                sla.Status = updateSLADto.Status;
            if (updateSLADto.Terms != null)
                sla.ServiceScope = updateSLADto.Terms;

            sla.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetSLAByIdAsync(id);
        }

        public async Task<bool> DeleteSLAAsync(int id)
        {
            var sla = await _context.SLAs.FindAsync(id);
            if (sla == null)
                return false;

            _context.SLAs.Remove(sla);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateSLAAsync(int id)
        {
            var sla = await _context.SLAs.FindAsync(id);
            if (sla == null)
                return false;

            sla.Status = "Active";
            sla.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateSLAAsync(int id)
        {
            var sla = await _context.SLAs.FindAsync(id);
            if (sla == null)
                return false;

            sla.Status = "Inactive";
            sla.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private SLADto MapToDto(SLA sla)
        {
            return new SLADto
            {
                Id = sla.Id,
                ClientId = sla.ClientId,
                ClientName = sla.Client?.Name,
                SLAType = sla.Title,
                ContractValue = sla.ContractValue,
                Escalation1DurationInHours = null,
                Escalation2DurationInHours = null,
                Escalation3DurationInHours = null,
                StartDate = sla.CreatedAt,
                RenewalDate = sla.RenewalDate,
                Status = sla.Status,
                Terms = sla.ServiceScope,
                CreatedBy = sla.CreatedBy,
                CreatedByName = sla.Creator?.Name,
                CreatedAt = sla.CreatedAt,
                UpdatedAt = sla.UpdatedAt
            };
        }
    }
}
