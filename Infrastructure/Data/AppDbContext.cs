using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {            
        }

        public DbSet<Role> Roles => Set<Role>();

        public DbSet<User> Users => Set<User>();

        public DbSet<Department> Departments => Set<Department>();

        public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();

        public DbSet<VacationBalance> VacationBalances => Set<VacationBalance>();

        public DbSet<VacationRequestApproval> VacationRequestApprovals => Set<VacationRequestApproval>();

        public DbSet<Status> Status => Set<Status>();

        public DbSet<ImportLog> ImportLogs => Set<ImportLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.PayRollNumber).IsUnique();

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<EmployeeProfile>(entity =>
            {
                entity.ToTable("EmployeeProfiles");

                entity.Property(e => e.DepartmentId)
                    .HasColumnName("departmentId");

                entity.Property(e => e.HireDate)
                    .HasColumnName("hireDate");

                entity.Property(e => e.ManagerId)
                    .HasColumnName("ManagerId");

                entity.HasOne(e => e.User)
                    .WithOne(u => u.EmployeeProfile)
                    .HasForeignKey<EmployeeProfile>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Manager)
                    .WithMany()
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VacationBalance>(entity =>
            {
                entity.HasIndex(v => new { v.UserId, v.Year }).IsUnique();

                entity.Property(v => v.Year)
                    .HasColumnName("year");

                entity.Property(v => v.AssignedDays)
                    .HasColumnName("assignedDays");

                entity.Property(v => v.UsedDays)
                    .HasColumnName("usedDays");

                entity.Property(v => v.CreatedAt)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.Property(v => v.StartDate)
                    .HasColumnName("startDate");

                entity.Property(v => v.EndDate)
                    .HasColumnName("endDate");

                entity.Property(v => v.RequestedDays)
                    .HasColumnName("requestedDays");

                entity.Property(v => v.StatusId)
                    .HasColumnName("statusId");

                entity.Property(v => v.CreatedAt)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAdd();

                entity.HasOne(v => v.User)
                .WithMany(u => u.VacationRequests)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VacationRequestApproval>(entity =>
            {
                entity.Property(a => a.VacationRequestId)
                    .HasColumnName("vacationRequestId");

                entity.Property(a => a.ApproverId)
                    .HasColumnName("approverId");

                entity.Property(a => a.ApprovalLevel)
                    .HasColumnName("approvalLevel");

                entity.Property(a => a.StatusId)
                    .HasColumnName("statusId");

                entity.Property(a => a.Comments)
                    .HasColumnName("comments");

                entity.Property(a => a.DecisionDate)
                    .HasColumnName("decisionDate");

                entity.HasOne(a => a.VacationRequest)
                    .WithMany(r => r.Approvals)
                    .HasForeignKey(a => a.VacationRequestId);

                entity.HasOne(a => a.Approver)
                    .WithMany()
                    .HasForeignKey(a => a.ApproverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ImportLog>(entity =>
            {
                entity.Property(i => i.ImportedBy)
                    .HasColumnName("importedBy");

                entity.Property(i => i.ImportedAt)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAdd();
       
                entity.HasOne(i => i.User)
                    .WithMany()
                    .HasForeignKey(i => i.ImportedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}