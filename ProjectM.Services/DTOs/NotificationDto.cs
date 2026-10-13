using System;

namespace ProjectM.Services.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
