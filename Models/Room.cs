using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models
{
    public class Room
    {
        public enum RoomType
        {
            NORMAL,
            STUDIO,
            SUITE
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Number { get; set; }
        [Required]
        public int Floor { get; set; }
        [Required]
        public RoomType Type { get; set; }
        [Required]
        public int Capacity { get; set; }
        [Required]
        [Precision(18, 2)]
        public decimal Price { get; set; }
        [Required]
        public bool Wifi { get; set; }
        [Required]
        public bool Breakfast { get; set; }
        [Required]
        public bool Smoking { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public int Dir { get; set; }
    }
}
