using System.ComponentModel.DataAnnotations;

namespace Project.Models
{
    public class User
    {
        [Key]
        public string? Email { get; set; }
        [Required]
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }
        [Required]
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
