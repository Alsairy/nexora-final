using Microsoft.EntityFrameworkCore;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexora.Core.Data
{
    public class NexoraDbContext : DbContext
    {
        private readonly ITenantService _tenantService;
        private readonly IAuditService _auditService;

        public NexoraDbContext(
            DbContextOptions<NexoraDbContext> options,
            ITenantService tenantService,
            IAuditService auditService) : base(options)
        {
            _tenantService = tenantService;
            _auditService = auditService;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure global query filter for soft delete
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Check if the entity implements ISoftDelete
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    // Apply global filter for soft delete
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                    var falseConstant = Expression.Constant(false);
                    var expression = Expression.Equal(property, falseConstant);
                    var lambda = Expression.Lambda(expression, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }

                // Apply multi-tenancy filter for tenant-specific entities
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // Configure tenant filter
                    var tenantFilterMethod = typeof(NexoraDbContext)
                        .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod(entityType.ClrType);

                    tenantFilterMethod.Invoke(this, new object[] { modelBuilder });
                }
            }

            // Special case for AuditLogEntry - apply global query filter
            modelBuilder.Entity<AuditLog>().HasQueryFilter(a => !a.IsDeleted);

            // Configure entity relationships and constraints
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.ReferenceId).IsUnique();
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Transactions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasIndex(e => e.TransactionId);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Transaction)
                    .WithMany(t => t.Payments)
                    .HasForeignKey(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasIndex(e => e.Subdomain).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(50);
            });
        }

        private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : class, ITenantEntity
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantService.GetCurrentTenantId());
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Get current tenant ID
            var tenantId = _tenantService.GetCurrentTenantId();
            
            // Handle audit entries
            var auditEntries = OnBeforeSaveChanges(tenantId);
            
            // Apply tenant ID to new entities
            ApplyTenantId(tenantId);
            
            // Apply soft delete
            ApplySoftDelete();
            
            var result = await base.SaveChangesAsync(cancellationToken);
            
            // Save audit logs after the main transaction completes
            await OnAfterSaveChanges(auditEntries);
            
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges(int tenantId)
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || !(entry.Entity is IAuditableEntity) || 
                    entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = _auditService.CreateAuditEntry(entry, tenantId);
                auditEntries.Add(auditEntry);
            }
            
            return auditEntries;
        }

        private void ApplyTenantId(int tenantId)
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.TenantId = tenantId;
                }
            }
        }

        private void ApplySoftDelete()
        {
            foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                }
            }
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            await _auditService.SaveAuditLogs(auditEntries);
        }
    }
}

