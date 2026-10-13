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
    public class PFIService : IPFIService
    {
        private readonly ApplicationDbContext _context;

        public PFIService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PFIDto> GetPFIByIdAsync(int id)
        {
            var pfi = await _context.PFIs
                .Include(p => p.Repair)
                .Include(p => p.Approver)
                .Include(p => p.LineItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            return pfi == null ? null : MapToDto(pfi);
        }

        public async Task<IEnumerable<PFIDto>> GetAllPFIsAsync()
        {
            var pfis = await _context.PFIs
                .Include(p => p.Repair)
                .Include(p => p.Approver)
                .Include(p => p.LineItems)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return pfis.Select(MapToDto);
        }

        public async Task<IEnumerable<PFIDto>> GetPFIsByRepairAsync(int repairId)
        {
            var pfis = await _context.PFIs
                .Include(p => p.Repair)
                .Include(p => p.Approver)
                .Include(p => p.LineItems)
                .Where(p => p.RepairId == repairId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return pfis.Select(MapToDto);
        }

        public async Task<IEnumerable<PFIDto>> GetPFIsByStatusAsync(string status)
        {
            var pfis = await _context.PFIs
                .Include(p => p.Repair)
                .Include(p => p.Approver)
                .Include(p => p.LineItems)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return pfis.Select(MapToDto);
        }

        public async Task<PFIDto> CreatePFIAsync(CreatePFIDto createPFIDto)
        {
            var pfi = new PFI
            {
                RepairId = createPFIDto.RepairId,
                PFINumber = GeneratePFINumber(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.PFIs.Add(pfi);
            await _context.SaveChangesAsync();

            // Add line items
            if (createPFIDto.LineItems != null && createPFIDto.LineItems.Any())
            {
                foreach (var item in createPFIDto.LineItems)
                {
                    var lineItem = new PFILineItem
                    {
                        PFIId = pfi.Id,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _context.PFILineItems.Add(lineItem);
                }
                await _context.SaveChangesAsync();
            }

            // Calculate total
            pfi.TotalAmount = await CalculateTotalAmount(pfi.Id);
            await _context.SaveChangesAsync();

            return await GetPFIByIdAsync(pfi.Id);
        }

        public async Task<PFIDto> UpdatePFIAsync(int id, UpdatePFIDto updatePFIDto)
        {
            var pfi = await _context.PFIs.Include(p => p.LineItems).FirstOrDefaultAsync(p => p.Id == id);
            if (pfi == null)
                return null;

            if (!string.IsNullOrEmpty(updatePFIDto.Status))
                pfi.Status = updatePFIDto.Status;

            // Update line items if provided
            if (updatePFIDto.LineItems != null)
            {
                // Remove old line items
                _context.PFILineItems.RemoveRange(pfi.LineItems);

                // Add new line items
                foreach (var item in updatePFIDto.LineItems)
                {
                    var lineItem = new PFILineItem
                    {
                        PFIId = pfi.Id,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _context.PFILineItems.Add(lineItem);
                }

                // Recalculate total
                await _context.SaveChangesAsync();
                pfi.TotalAmount = await CalculateTotalAmount(pfi.Id);
            }

            pfi.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetPFIByIdAsync(id);
        }

        public async Task<bool> ApprovePFIAsync(int id, int approvedBy)
        {
            var pfi = await _context.PFIs.FindAsync(id);
            if (pfi == null)
                return false;

            pfi.Status = "Approved";
            pfi.ApprovedBy = approvedBy;
            pfi.ApprovedAt = DateTime.UtcNow;
            pfi.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePFIAsync(int id)
        {
            var pfi = await _context.PFIs.Include(p => p.LineItems).FirstOrDefaultAsync(p => p.Id == id);
            if (pfi == null)
                return false;

            _context.PFILineItems.RemoveRange(pfi.LineItems);
            _context.PFIs.Remove(pfi);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GeneratePFINumber()
        {
            return $"PFI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        private async Task<decimal> CalculateTotalAmount(int pfiId)
        {
            return await _context.PFILineItems
                .Where(li => li.PFIId == pfiId)
                .SumAsync(li => li.Quantity * li.UnitPrice);
        }

        private PFIDto MapToDto(PFI pfi)
        {
            return new PFIDto
            {
                Id = pfi.Id,
                RepairId = pfi.RepairId,
                PFINumber = pfi.PFINumber,
                TotalAmount = pfi.TotalAmount,
                Status = pfi.Status,
                ApprovedBy = pfi.ApprovedBy,
                ApprovedByName = pfi.Approver?.Name ?? "",
                ApprovedAt = pfi.ApprovedAt,
                CreatedAt = pfi.CreatedAt,
                UpdatedAt = pfi.UpdatedAt,
                LineItems = pfi.LineItems?.Select(li => new PFILineItemDto
                {
                    Id = li.Id,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    TotalPrice = li.Quantity * li.UnitPrice
                }).ToList()
            };
        }
    }
}