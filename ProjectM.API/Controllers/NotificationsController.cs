using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectM.API.Authorization;
using ProjectM.Services.DTOs;
using ProjectM.Services.Interfaces;
using System.Security.Claims;

namespace ProjectM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.GetUserId();
            var notifications = await _notificationService.GetNotificationsByUserAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = User.GetUserId();
            var notifications = await _notificationService.GetUnreadNotificationsByUserAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(int id)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(id);
            if (notification == null || notification.UserId != User.GetUserId())
                return NotFound();

            return Ok(notification);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto createNotificationDto)
        {
            var notification = await _notificationService.CreateNotificationAsync(createNotificationDto);
            if (notification == null)
                return BadRequest(new { message = "Unable to create notification." });

            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
        }

        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var owned = await _notificationService.GetNotificationByIdAsync(id);
            if (owned == null || owned.UserId != User.GetUserId())
                return NotFound();

            var result = await _notificationService.MarkAsReadAsync(id);
            if (!result)
                return NotFound();

            return Ok(new { message = "Notification marked as read" });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "All notifications marked as read" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var owned = await _notificationService.GetNotificationByIdAsync(id);
            if (owned == null || owned.UserId != User.GetUserId())
                return NotFound();

            var result = await _notificationService.DeleteNotificationAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
