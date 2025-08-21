using Microsoft.EntityFrameworkCore;
using hrms.Models;

namespace hrms.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- THIS IS THE FINAL, DETAILED CONFIGURATION ---
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.Projects)
                .WithMany(p => p.AssignedEmployees)
                .UsingEntity<Dictionary<string, object>>(
                    "project_assignments", // The name of the junction table
                    j => j
                        .HasOne<Project>()
                        .WithMany()
                        .HasForeignKey("project_id"), // Explicitly name the FK to Project
                    j => j
                        .HasOne<Employee>()
                        .WithMany()
                        .HasForeignKey("employee_id") // Explicitly name the FK to Employee
                );
            // --- END FINAL CONFIGURATION ---
        }
    }
}