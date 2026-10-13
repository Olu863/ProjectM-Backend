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
    public class RepairService : IRepairService
    {
        private readonly ApplicationDbContext _context;

        public RepairService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RepairDto> GetRepairByIdAsync(int id)
        {
            var repair = await _context.Repairs
                .Include(r => r.Client)
                .Include(r => r.Technician)
                .Include(r => r.Creator)
                .FirstOrDefaultAsync(r => r.Id == id);

            return repair == null ? null : MapToDto(repair);
        }

        public async Task<IEnumerable<RepairDto>> GetAllRepairsAsync()
        {
            var repairs = await _context.Repairs
                .Include(r => r.Client)
                .Include(r => r.Technician)
                .Include(r => r.Creator)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return repairs.Select(MapToDto);
        }

        public async Task<IEnumerable<RepairDto>> GetRepairsByClientAsync(int clientId)
        {
            var repairs = await _context.Repairs
                .Include(r => r.Client)
                .Include(r => r.Technician)
                .Include(r => r.Creator)
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return repairs.Select(MapToDto);
        }

        public async Task<IEnumerable<RepairDto>> GetRepairsByTechnicianAsync(int technicianId)
        {
            var repairs = await _context.Repairs
                .Include(r => r.Client)
                .Include(r => r.Technician)
                .Include(r => r.Creator)
                .Where(r => r.TechnicianId == technicianId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return repairs.Select(MapToDto);
        }

        public async Task<IEnumerable<RepairDto>> GetRepairsByStatusAsync(string status)
        {
            var repairs = await _context.Repairs
                .Include(r => r.Client)
                .Include(r => r.Technician)
                .Include(r => r.Creator)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return repairs.Select(MapToDto);
        }

        public async Task<RepairDto> CreateRepairAsync(CreateRepairDto createRepairDto, int createdBy)
        {
            var repair = new Repair
            {
                ClientId = createRepairDto.ClientId,
                EquipmentName = createRepairDto.EquipmentName,
                EquipmentSerial = createRepairDto.EquipmentSerial,
                IssueDescription = createRepairDto.IssueDescription,
                Priority = createRepairDto.Priority,
                Status = "Requested",
                CostEstimate = createRepairDto.CostEstimate,
                DowntimeProjection = createRepairDto.DowntimeProjection,
                EstimatedCompletionDate = createRepairDto.EstimatedCompletionDate,
                Notes = createRepairDto.Notes,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.Repairs.Add(repair);
            await _context.SaveChangesAsync();

            return await GetRepairByIdAsync(repair.Id);
        }

        public async Task<RepairDto> UpdateRepairAsync(int id, UpdateRepairDto updateRepairDto)
        {
            var repair = await _context.Repairs.FindAsync(id);
            if (repair == null)
                return null;

            if (updateRepairDto.TechnicianId.HasValue)
                repair.TechnicianId = updateRepairDto.TechnicianId;
            if (!string.IsNullOrEmpty(updateRepairDto.EquipmentName))
                repair.EquipmentName = updateRepairDto.EquipmentName;
            if (!string.IsNullOrEmpty(updateRepairDto.EquipmentSerial))
                repair.EquipmentSerial = updateRepairDto.EquipmentSerial;
            if (!string.IsNullOrEmpty(updateRepairDto.IssueDescription))
                repair.IssueDescription = updateRepairDto.IssueDescription;
            if (!string.IsNullOrEmpty(updateRepairDto.Priority))
                repair.Priority = updateRepairDto.Priority;
            if (!string.IsNullOrEmpty(updateRepairDto.Status))
                repair.Status = updateRepairDto.Status;
            if (updateRepairDto.CostEstimate.HasValue)
                repair.CostEstimate = updateRepairDto.CostEstimate;
            if (updateRepairDto.ActualCost.HasValue)
                repair.ActualCost = updateRepairDto.ActualCost;
            if (updateRepairDto.DowntimeProjection.HasValue)
                repair.DowntimeProjection = updateRepairDto.DowntimeProjection;
            if (updateRepairDto.ActualDowntime.HasValue)
                repair.ActualDowntime = updateRepairDto.ActualDowntime;
            if (updateRepairDto.EstimatedCompletionDate.HasValue)
                repair.EstimatedCompletionDate = updateRepairDto.EstimatedCompletionDate;
            if (updateRepairDto.ActualCompletionDate.HasValue)
                repair.ActualCompletionDate = updateRepairDto.ActualCompletionDate;
            if (updateRepairDto.Notes != null)
                repair.Notes = updateRepairDto.Notes;

            repair.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetRepairByIdAsync(id);
        }

        public async Task<bool> DeleteRepairAsync(int id)
        {
            var repair = await _context.Repairs.FindAsync(id);
            if (repair == null)
                return false;

            _context.Repairs.Remove(repair);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignTechnicianAsync(int repairId, int technicianId)
        {
            var repair = await _context.Repairs.FindAsync(repairId);
            if (repair == null)
                return false;

            repair.TechnicianId = technicianId;
            repair.Status = "In_Progress";
            repair.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int repairId, string status)
        {
            var repair = await _context.Repairs.FindAsync(repairId);
            if (repair == null)
                return false;

            repair.Status = status;
            repair.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteRepairAsync(int repairId, decimal actualCost, int actualDowntime)
        {
            var repair = await _context.Repairs.FindAsync(repairId);
            if (repair == null)
                return false;

            repair.Status = "Completed";
            repair.ActualCost = actualCost;
            repair.ActualDowntime = actualDowntime;
            repair.ActualCompletionDate = DateTime.UtcNow;
            repair.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private RepairDto MapToDto(Repair repair)
        {
            return new RepairDto
            {
                Id = repair.Id,
                ClientId = repair.ClientId,
                ClientName = repair.Client?.Name ?? "",
                TechnicianId = repair.TechnicianId,
                TechnicianName = repair.Technician?.Name ?? "",
                EquipmentName = repair.EquipmentName,
                EquipmentSerial = repair.EquipmentSerial ?? "",
                IssueDescription = repair.IssueDescription,
                Priority = repair.Priority,
                Status = repair.Status,
                PFIUrl = repair.PFIUrl ?? "",
                CostEstimate = repair.CostEstimate,
                ActualCost = repair.ActualCost,
                DowntimeProjection = repair.DowntimeProjection,
                ActualDowntime = repair.ActualDowntime,
                EstimatedCompletionDate = repair.EstimatedCompletionDate,
                ActualCompletionDate = repair.ActualCompletionDate,
                Notes = repair.Notes ?? "",
                CreatedBy = repair.CreatedBy,
                CreatedByName = repair.Creator?.Name ?? "",
                CreatedAt = repair.CreatedAt,
                UpdatedAt = repair.UpdatedAt
            };
        }
    }
}