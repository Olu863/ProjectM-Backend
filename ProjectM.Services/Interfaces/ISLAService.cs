using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectM.Services.DTOs;

namespace ProjectM.Services.Interfaces
{
    public interface ISLAService
    {
        Task<SLADto> GetSLAByIdAsync(int id);
        Task<IEnumerable<SLADto>> GetAllSLAsAsync();
        Task<IEnumerable<SLADto>> GetSLAsByClientAsync(int clientId);
        Task<IEnumerable<SLADto>> GetSLAsByStatusAsync(string status);
        Task<SLADto> CreateSLAAsync(CreateSLADto createSLADto, int createdBy);
        Task<SLADto> UpdateSLAAsync(int id, UpdateSLADto updateSLADto);
        Task<bool> DeleteSLAAsync(int id);
        Task<bool> ActivateSLAAsync(int id);
        Task<bool> DeactivateSLAAsync(int id);
    }
}
