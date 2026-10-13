namespace ProjectM.Services.DTOs
{
    public class UpdateUserDto
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}
