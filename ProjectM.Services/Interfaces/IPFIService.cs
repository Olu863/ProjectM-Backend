using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectM.Services.DTOs;

namespace ProjectM.Services.Interfaces
{
    public interface IPFIService
    {
        Task<PFIDto> GetPFIByIdAsync(int id);
        Task<IEnumerable<PFIDto>> GetAllPFIsAsync();
        Task<IEnumerable<PFIDto>> GetPFIsByRepairAsync(int repairId);
        Task<IEnumerable<PFIDto>> GetPFIsByStatusAsync(string status);
        Task<PFIDto> CreatePFIAsync(CreatePFIDto createPFIDto);
        Task<PFIDto> UpdatePFIAsync(int id, UpdatePFIDto updatePFIDto);
        Task<bool> ApprovePFIAsync(int id, int approvedBy);
        Task<bool> DeletePFIAsync(int id);
    }
}
