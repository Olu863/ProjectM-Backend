using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectM.Services.DTOs;

namespace ProjectM.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> GetNotificationByIdAsync(int id);
        Task<IEnumerable<NotificationDto>> GetNotificationsByUserAsync(int userId);
        Task<IEnumerable<NotificationDto>> GetUnreadNotificationsByUserAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto createNotificationDto);
        Task<bool> MarkAsReadAsync(int id);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int id);
    }
}
