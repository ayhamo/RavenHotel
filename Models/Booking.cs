using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Project.Models
{
    public class Booking
    {
        [Key]
        public string? Confirmation { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckIn { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckOut { get; set; }
        [Required]
        public int People { get; set; }
        [Required]
        [Precision(18, 2)]
        public decimal Price { get; set; }
        [Required]
        public User? User { get; set; }
        [Required]
        public Room? Room { get; set; }
    }
}
