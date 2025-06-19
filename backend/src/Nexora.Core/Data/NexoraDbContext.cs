using Microsoft.EntityFrameworkCore;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        public DbSet<SmsMessage> SmsMessages { get; set; }
        public DbSet<SmsTemplate> SmsTemplates { get; set; }
        public DbSet<SmsProvider> SmsProviders { get; set; }
        public DbSet<SenderId> SenderIds { get; set; }
        public DbSet<SmsCompliance> SmsCompliances { get; set; }
        public DbSet<SmsAnalytics> SmsAnalytics { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<RecurringPayment> RecurringPayments { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<PaymentProvider> PaymentProviders { get; set; }
        public DbSet<SmsBilling> SmsBillings { get; set; }
        public DbSet<PaymentAnalytics> PaymentAnalytics { get; set; }
        public DbSet<TransactionAnalytics> TransactionAnalytics { get; set; }
        public DbSet<RevenueAnalytics> RevenueAnalytics { get; set; }
        public DbSet<ComplianceReport> ComplianceReports { get; set; }
        public DbSet<ExportJob> ExportJobs { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }

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

            // Configure SMS Gateway entities
            modelBuilder.Entity<SmsMessage>(entity =>
            {
                entity.HasIndex(e => e.MessageId).IsUnique();
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1600);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.SmsMessages)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.SmsProvider)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SmsTemplate>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1600);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.SmsTemplates)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SmsProvider>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApiEndpoint).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<SenderId>(entity =>
            {
                entity.Property(e => e.SenderId).IsRequired().HasMaxLength(11);
                entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.SenderId, e.TenantId }).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.SenderIds)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SmsCompliance>(entity =>
            {
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1600);
                entity.HasOne(e => e.SmsMessage)
                    .WithOne(m => m.ComplianceCheck)
                    .HasForeignKey<SmsCompliance>(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SmsAnalytics>(entity =>
            {
                entity.Property(e => e.MessageCount).HasDefaultValue(0);
                entity.Property(e => e.DeliveredCount).HasDefaultValue(0);
                entity.Property(e => e.FailedCount).HasDefaultValue(0);
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18,4)").HasDefaultValue(0);
            });

            // Configure Fintech entities
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Subscriptions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RecurringPayment>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.RecurringPayments)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PaymentMethod)
                    .WithMany(pm => pm.RecurringPayments)
                    .HasForeignKey(e => e.PaymentMethodId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastFourDigits).HasMaxLength(4);
                entity.Property(e => e.ExpiryMonth).HasMaxLength(2);
                entity.Property(e => e.ExpiryYear).HasMaxLength(4);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.PaymentMethods)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentProvider>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
                entity.Property(e => e.CountryCode).HasMaxLength(3);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<SmsBilling>(entity =>
            {
                entity.Property(e => e.MessageCount).HasDefaultValue(0);
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18,4)").HasDefaultValue(0);
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("SAR");
                entity.HasOne(e => e.User)
                    .WithMany(u => u.SmsBillings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentAnalytics>(entity =>
            {
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.TransactionCount).HasDefaultValue(0);
                entity.Property(e => e.SuccessRate).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("SAR");
            });

            modelBuilder.Entity<TransactionAnalytics>(entity =>
            {
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.TransactionCount).HasDefaultValue(0);
                entity.Property(e => e.AverageAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("SAR");
            });

            modelBuilder.Entity<RevenueAnalytics>(entity =>
            {
                entity.Property(e => e.TotalRevenue).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.RecurringRevenue).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.OneTimeRevenue).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("SAR");
            });

            modelBuilder.Entity<ComplianceReport>(entity =>
            {
                entity.Property(e => e.ReportType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FilePath).HasMaxLength(500);
            });

            modelBuilder.Entity<ExportJob>(entity =>
            {
                entity.Property(e => e.JobType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Format).IsRequired().HasMaxLength(10);
                entity.Property(e => e.FilePath).HasMaxLength(500);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            });

            modelBuilder.Entity<NotificationLog>(entity =>
            {
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Channel).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.NotificationLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
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

