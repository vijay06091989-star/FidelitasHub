using FidelitasHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        //==================================================
        // Masters
        //==================================================

        public DbSet<Department> Departments { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Shift> Shifts { get; set; }

        //==================================================
        // Attendance
        //==================================================

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<AttendanceBreak> AttendanceBreaks { get; set; }

        //==================================================
        // Audit
        //==================================================

        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}