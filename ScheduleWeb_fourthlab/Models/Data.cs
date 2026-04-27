using Microsoft.EntityFrameworkCore;
using ScheduleWeb.Models;

namespace ScheduleWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<StudyGroup> StudyGroups { get; set; }
        public DbSet<Lesson> Lesson { get; set; }
    }
}