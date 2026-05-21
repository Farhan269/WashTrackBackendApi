using Microsoft.EntityFrameworkCore;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ProcessStage> ProcessStages { get; set; }
        public DbSet<UserProcessStageAccess> UserProcessStageAccesses { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<WashTransaction> WashTransactions { get; set; }
        public DbSet<ProcessStageBalance> ProcessStageBalances { get; set; }
        public DbSet<ShiftSchedule> ShiftSchedules { get; set; }
        // ✅ NEW: Add SyncLog DbSet
        public DbSet<SyncLog> SyncLogs { get; set; }

        public DbSet<Plant> Plants { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UserAssign> UserAssigns { get; set; }

        public DbSet<WashMachine> WashMachine { get; set; }
        public DbSet<WashMachineAssign> WashMachineAssign { get; set; }
        public DbSet<WashPlan> WashPlan { get; set; }
        public DbSet<WashPlanMachine> WashPlanMachine { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // USER
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // ROLE
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // USER-ROLE
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PROCESS STAGE
            modelBuilder.Entity<ProcessStage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.DisplayOrder);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // USER PROCESS STAGE ACCESS
            modelBuilder.Entity<UserProcessStageAccess>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.ProcessStageId }).IsUnique();

                entity.HasOne(ups => ups.User)
                    .WithMany(u => u.UserProcessStageAccesses)
                    .HasForeignKey(ups => ups.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ups => ups.ProcessStage)
                    .WithMany(ps => ps.UserProcessStageAccesses)
                    .HasForeignKey(ups => ups.ProcessStageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WASH TRANSACTION
            modelBuilder.Entity<WashTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WorkOrderId);
                entity.HasIndex(e => new { e.WorkOrderId, e.ProcessStageId, e.TransactionType });
                entity.HasIndex(e => e.ProcessStageId);

                entity.Property(e => e.TransactionType)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasOne(t => t.WorkOrder)
                    .WithMany()
                    .HasForeignKey(t => t.WorkOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.ProcessStage)
                    .WithMany(ps => ps.WashTransactions)
                    .HasForeignKey(t => t.ProcessStageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                // Map Enum to TINYINT in database
                entity.Property(e => e.ShiftType)
                      .HasConversion<byte>()
                      .IsRequired();

                


                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // PROCESS STAGE BALANCE
            modelBuilder.Entity<ProcessStageBalance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.WorkOrderId, e.ProcessStageId }).IsUnique();

                entity.HasOne(s => s.WorkOrder)
                    .WithMany()
                    .HasForeignKey(s => s.WorkOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.ProcessStage)
                    .WithMany(ps => ps.ProcessStageBalances)
                    .HasForeignKey(s => s.ProcessStageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
            });

            // WORK ORDER
            modelBuilder.Entity<WorkOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WorkOrderNo).IsUnique();
                entity.HasIndex(e => new { e.Factory, e.Line, e.WorkOrderNo });

                entity.HasOne(w => w.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(w => w.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(w => w.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(w => w.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // ✅ NEW: Configure SyncLog
            modelBuilder.Entity<SyncLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SyncType);
                entity.HasIndex(e => e.CreatedAt);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

         

            // SHIFT SCHEDULE
            modelBuilder.Entity<ShiftSchedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EffectiveFromDate);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // PLANT
            modelBuilder.Entity<Plant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.isDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Name).IsUnique().HasFilter("[IsDeleted] = 0");
            });

            // UNIT
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PlantId).IsRequired();
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.PlantId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");

                entity.HasOne<Plant>()
                    .WithMany()
                    .HasForeignKey(e => e.PlantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // USER ASSIGN
            modelBuilder.Entity<UserAssign>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PlantId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UnitId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.isDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.UserId, e.PlantId, e.UnitId }).IsUnique().HasFilter("[IsDeleted] = 0");

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WASH MACHINE
            modelBuilder.Entity<WashMachine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MachineCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Brand).HasMaxLength(100);
                entity.Property(e => e.Model).HasMaxLength(100);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
              

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WASH MACHINE ASSIGN
            modelBuilder.Entity<WashMachineAssign>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MachineId).IsRequired();
                entity.Property(e => e.PlantId).IsRequired();
                entity.Property(e => e.UnitId).IsRequired();
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.MachineId, e.PlantId, e.UnitId }).IsUnique().HasFilter("[IsDeleted] = 0");

                entity.HasOne<WashMachine>()
                    .WithMany()
                    .HasForeignKey(e => e.MachineId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Plant>()
                    .WithMany()
                    .HasForeignKey(e => e.PlantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Unit>()
                    .WithMany()
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WASH Plan MACHINE
            modelBuilder.Entity<WashPlanMachine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WashPlanId).IsRequired();
                entity.Property(e => e.MachineId).IsRequired();
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");


                // =========================
                // RELATION: WASH PLAN
                // =========================
                entity.HasOne<WashPlan>()
                    .WithMany()
                    .HasForeignKey(e => e.WashPlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                // =========================
                // RELATION: MACHINE
                // =========================
                entity.HasOne<WashMachine>()
                    .WithMany()
                    .HasForeignKey(e => e.MachineId)
                    .OnDelete(DeleteBehavior.Restrict);

                // =========================
                // CREATED BY
                // =========================
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WASH PLAN
            modelBuilder.Entity<WashPlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WorkOrderId).IsRequired();
                entity.Property(e => e.ProcessStageId).IsRequired();
                entity.Property(e => e.PlanDate).IsRequired();
                entity.Property(e => e.Shift).IsRequired();
                entity.Property(e => e.PlantId).IsRequired();
                entity.Property(e => e.UnitId).IsRequired();
     
                entity.Property(e => e.BaseTargetQty).HasPrecision(18, 2);
                entity.Property(e => e.Percentage).HasPrecision(18, 2);
                entity.Property(e => e.AdjustedTargetQty).HasPrecision(18, 2);
                entity.Property(e => e.FinalTargetQty).HasPrecision(18, 2);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => new { e.WorkOrderId, e.ProcessStageId,e.PlanDate, e.Shift, e.PlantId, e.UnitId });

                entity.HasOne<WorkOrder>()
                    .WithMany()
                    .HasForeignKey(e => e.WorkOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<ProcessStage>()
                    .WithMany()
                    .HasForeignKey(e => e.ProcessStageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Plant>()
                    .WithMany()
                    .HasForeignKey(e => e.PlantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Unit>()
                    .WithMany()
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Full system access", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = 2, Name = "User", Description = "Limited access based on stage", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<ProcessStage>().HasData(
                new ProcessStage { Id = 1, Name = "1st Dry", Description = "First Dry Process", DisplayOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ProcessStage { Id = 2, Name = "Unwash", Description = "Unwash Process", DisplayOrder = 2, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ProcessStage { Id = 3, Name = "2nd Dry", Description = "Second Dry Process", DisplayOrder = 3, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ProcessStage { Id = 4, Name = "1st Wash", Description = "First Wash Process", DisplayOrder = 4, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ProcessStage { Id = 5, Name = "Final Wash", Description = "Final Wash Process", DisplayOrder = 5, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}