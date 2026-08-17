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

        public DbSet<Designation> Designations { get; set; }

        public DbSet<LeaveType> LeaveTypes { get; set; }

        public DbSet<Holiday> Holidays { get; set; }

        //==================================================
        // Attendance
        //==================================================

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<AttendanceBreak> AttendanceBreaks { get; set; }

        //==================================================
        // Leave Management
        //==================================================

        public DbSet<LeaveApplication> LeaveApplications { get; set; }

        public DbSet<LeaveBalanceLedger> LeaveBalanceLedgers { get; set; }

        public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances { get; set; }

        public DbSet<PayrollCalendar> PayrollCalendars { get; set; }

        //==================================================
        // Audit
        //==================================================

        public DbSet<AuditLog> AuditLogs { get; set; }

        //==================================================
        // System Settings
        //==================================================

        public DbSet<SystemSetting> SystemSettings { get; set; }

        public DbSet<EmailTemplate> EmailTemplates { get; set; }


        //==================================================
        // Entity Relationships
        //==================================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //==================================================
            // Reporting Manager relationship
            //==================================================

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ReportingManager)
                .WithMany()
                .HasForeignKey(e => e.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);


            //==================================================
            // Reporting Team Leader relationship
            //==================================================

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ReportingTeamLeader)
                .WithMany()
                .HasForeignKey(e => e.ReportingTeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}