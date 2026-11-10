namespace ProjectM.Services.DTOs
{
    public class UpdateUserDto
    {
        // Every field is optional: only the fields that are sent are changed.
        public string? Name { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }      // honoured for Admins only
        public bool? IsActive { get; set; }    // honoured for Admins only
    }
}
