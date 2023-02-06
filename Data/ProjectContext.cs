using Microsoft.EntityFrameworkCore;
using Project.Models;

namespace Project.Data
{
    public class ProjectContext : DbContext
    {
        public ProjectContext(DbContextOptions<ProjectContext> options)
            : base(options)
        {
        }
        public DbSet<Booking> Bookings { get; set; } = default!;
        public DbSet<Room> Rooms { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
    }
}
