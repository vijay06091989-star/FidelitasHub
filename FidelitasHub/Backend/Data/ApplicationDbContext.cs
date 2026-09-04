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

        public DbSet<Client> Clients { get; set; }

        public DbSet<ClientSopDocument> ClientSopDocuments { get; set; }


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
            // Employee Reporting Relationships
            //==================================================

            // Reporting Manager
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ReportingManager)
                .WithMany()
                .HasForeignKey(e => e.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Reporting Team Leader
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ReportingTeamLeader)
                .WithMany()
                .HasForeignKey(e => e.ReportingTeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict);


            //==================================================
            // Client Responsibility Relationships
            //==================================================

            // General Shift Manager
            modelBuilder.Entity<Client>()
                .HasOne(c => c.GeneralShiftManager)
                .WithMany()
                .HasForeignKey(c => c.GeneralShiftManagerId)
                .OnDelete(DeleteBehavior.Restrict);


            // General Shift Team Leader - Billing
            modelBuilder.Entity<Client>()
                .HasOne(c => c.GeneralShiftBillingTeamLeader)
                .WithMany()
                .HasForeignKey(c => c.GeneralShiftBillingTeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict);


            // General Shift Team Leader - Posting
            modelBuilder.Entity<Client>()
                .HasOne(c => c.GeneralShiftPostingTeamLeader)
                .WithMany()
                .HasForeignKey(c => c.GeneralShiftPostingTeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict);


            // General Shift Team Leader - DM
            modelBuilder.Entity<Client>()
                .HasOne(c => c.GeneralShiftDMTeamLeader)
                .WithMany()
                .HasForeignKey(c => c.GeneralShiftDMTeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict);


            // General Shift Team Leader - End to End
            modelBuilder.Entity<Client>()
                .HasOne(c => c.GeneralShiftEndToEndTeamLeader)
                .WithMany()
                .HasForeignKey(c => c.GeneralShiftEndToEndTeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict);


            // US Shift Manager
            modelBuilder.Entity<Client>()
                .HasOne(c => c.USShiftManager)
                .WithMany()
                .HasForeignKey(c => c.USShiftManagerId)
                .OnDelete(DeleteBehavior.Restrict);


            // US Shift Team Leader
            modelBuilder.Entity<Client>()
                .HasOne(c => c.USShiftTeamLeader)
                .WithMany()
                .HasForeignKey(c => c.USShiftTeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict);


            //==================================================
            // Client SOP Documents
            //==================================================

            modelBuilder.Entity<ClientSopDocument>()
                .HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientSopDocument>()
                .HasIndex(s => new { s.ClientId, s.Version })
                .IsUnique();
        }
    }
}