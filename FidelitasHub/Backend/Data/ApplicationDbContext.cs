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

        public DbSet<ClientWebLogin> ClientWebLogins { get; set; }

        public DbSet<ProductivityProcess> ProductivityProcesses { get; set; }
        public DbSet<ProductivityActivity> ProductivityActivities { get; set; }
        public DbSet<ProductivityAssignment> ProductivityAssignments { get; set; }
        public DbSet<ProductivityEntry> ProductivityEntries { get; set; }
        public DbSet<ProductivityGroup> ProductivityGroups { get; set; }
        public DbSet<ProductivityGroupMember> ProductivityGroupMembers { get; set; }
        public DbSet<ProductivityGroupAssignment> ProductivityGroupAssignments { get; set; }


        //==================================================
        // Attendance
        //==================================================

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<AttendanceBreak> AttendanceBreaks { get; set; }

        public DbSet<EmployeeIdleSession> EmployeeIdleSessions { get; set; }


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
            // Employee Idle Monitoring Relationships
            //==================================================

            modelBuilder.Entity<EmployeeIdleSession>()
                .HasOne(i => i.Employee)
                .WithMany()
                .HasForeignKey(i => i.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeIdleSession>()
                .HasOne(i => i.Attendance)
                .WithMany()
                .HasForeignKey(i => i.AttendanceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeIdleSession>()
                .HasIndex(i => new { i.AttendanceId, i.IdleEnd });

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


            //==================================================
            // Productivity
            //==================================================

            modelBuilder.Entity<ProductivityProcess>()
                .HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductivityProcess>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityProcess>()
                .HasIndex(x => new { x.ClientId, x.ProcessCode }).IsUnique();

            modelBuilder.Entity<ProductivityActivity>()
                .HasOne(x => x.Process).WithMany(x => x.Activities).HasForeignKey(x => x.ProductivityProcessId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductivityActivity>()
                .HasIndex(x => new { x.ProductivityProcessId, x.ActivityCode }).IsUnique();

            modelBuilder.Entity<ProductivityAssignment>()
                .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityAssignment>()
                .HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityAssignment>()
                .HasOne(x => x.Activity).WithMany().HasForeignKey(x => x.ProductivityActivityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityAssignment>()
                .HasIndex(x => new { x.EmployeeId, x.ClientId, x.ProductivityActivityId, x.EffectiveFrom });

            modelBuilder.Entity<ProductivityEntry>()
                .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityEntry>()
                .HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityEntry>()
                .HasOne(x => x.Activity).WithMany().HasForeignKey(x => x.ProductivityActivityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityEntry>()
                .HasOne(x => x.EnteredByEmployee).WithMany().HasForeignKey(x => x.EnteredByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityEntry>()
                .HasIndex(x => new { x.ProductionDate, x.EmployeeId, x.ClientId, x.ProductivityActivityId });

            // Explicit SQL precision for Productivity decimal values.
            modelBuilder.Entity<ProductivityActivity>()
                .Property(x => x.DefaultTargetPerDay).HasPrecision(18, 4);
            modelBuilder.Entity<ProductivityActivity>()
                .Property(x => x.DefaultWeight).HasPrecision(18, 6);
            modelBuilder.Entity<ProductivityAssignment>()
                .Property(x => x.TargetPerDay).HasPrecision(18, 4);
            modelBuilder.Entity<ProductivityAssignment>()
                .Property(x => x.Weight).HasPrecision(18, 6);
            modelBuilder.Entity<ProductivityEntry>()
                .Property(x => x.Quantity).HasPrecision(18, 4);
            modelBuilder.Entity<ProductivityEntry>()
                .Property(x => x.TargetPerDaySnapshot).HasPrecision(18, 4);
            modelBuilder.Entity<ProductivityEntry>()
                .Property(x => x.WeightSnapshot).HasPrecision(18, 6);
            modelBuilder.Entity<ProductivityEntry>()
                .Property(x => x.WeightedAchievement).HasPrecision(18, 4);
            modelBuilder.Entity<ProductivityGroupAssignment>()
                .Property(x => x.TargetPerDay).HasPrecision(18, 4);
            modelBuilder.Entity<ProductivityGroupAssignment>()
                .Property(x => x.Weight).HasPrecision(18, 6);

            modelBuilder.Entity<ProductivityGroup>()
                .HasIndex(x => x.GroupName).IsUnique();

            modelBuilder.Entity<ProductivityGroupMember>()
                .HasOne(x => x.Group).WithMany(x => x.Members)
                .HasForeignKey(x => x.ProductivityGroupId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductivityGroupMember>()
                .HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityGroupMember>()
                .HasIndex(x => new { x.ProductivityGroupId, x.EmployeeId, x.EffectiveFrom });

            modelBuilder.Entity<ProductivityGroupAssignment>()
                .HasOne(x => x.Group).WithMany(x => x.Assignments)
                .HasForeignKey(x => x.ProductivityGroupId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductivityGroupAssignment>()
                .HasOne(x => x.Client).WithMany()
                .HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityGroupAssignment>()
                .HasOne(x => x.Activity).WithMany()
                .HasForeignKey(x => x.ProductivityActivityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductivityGroupAssignment>()
                .HasIndex(x => new { x.ProductivityGroupId, x.ClientId, x.ProductivityActivityId, x.EffectiveFrom });

            //==================================================
            // Client Web Portal Logins
            //==================================================

            modelBuilder.Entity<ClientWebLogin>()
                .HasOne(w => w.Client)
                .WithMany()
                .HasForeignKey(w => w.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientWebLogin>()
                .HasIndex(w => w.ClientId);
        }
    }
}