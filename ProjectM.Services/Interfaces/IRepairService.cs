using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectM.Services.DTOs;

namespace ProjectM.Services.Interfaces
{
    public interface IRepairService
    {
        Task<RepairDto> GetRepairByIdAsync(int id);
        Task<IEnumerable<RepairDto>> GetAllRepairsAsync();
        Task<IEnumerable<RepairDto>> GetRepairsByClientAsync(int clientId);
        Task<IEnumerable<RepairDto>> GetRepairsByTechnicianAsync(int technicianId);
        Task<IEnumerable<RepairDto>> GetRepairsByStatusAsync(string status);
        Task<RepairDto> CreateRepairAsync(CreateRepairDto createRepairDto, int createdBy);
        Task<RepairDto> UpdateRepairAsync(int id, UpdateRepairDto updateRepairDto);
        Task<bool> DeleteRepairAsync(int id);
        Task<bool> AssignTechnicianAsync(int repairId, int technicianId);
        Task<bool> UpdateStatusAsync(int repairId, string status);
        Task<bool> CompleteRepairAsync(int repairId, decimal actualCost, int actualDowntime);
    }
}
