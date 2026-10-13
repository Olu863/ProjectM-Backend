namespace ProjectM.Services.DTOs
{
    public class CreateUserDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
    }
}
