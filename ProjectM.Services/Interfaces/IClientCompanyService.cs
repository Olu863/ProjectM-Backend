using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectM.Services.DTOs;

namespace ProjectM.Services.Interfaces
{
    public interface IClientCompanyService
    {
        Task<ClientCompanyDto> GetClientCompanyByIdAsync(int id);
        Task<ClientCompanyDto> GetClientCompanyByUserIdAsync(int userId);
        Task<IEnumerable<ClientCompanyDto>> GetAllClientCompaniesAsync();
        Task<IEnumerable<ClientCompanyDto>> GetClientCompaniesByStateAsync(string state);
        Task<ClientCompanyDto> CreateClientCompanyAsync(CreateClientCompanyDto createClientCompanyDto);
        Task<ClientCompanyDto> UpdateClientCompanyAsync(int id, UpdateClientCompanyDto updateClientCompanyDto);
        Task<bool> DeleteClientCompanyAsync(int id);
    }
}
