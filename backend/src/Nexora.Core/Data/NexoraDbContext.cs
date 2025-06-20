using Microsoft.EntityFrameworkCore;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.Models;
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
        public NexoraDbContext(DbContextOptions<NexoraDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        
        public DbSet<ESignatureDocument> ESignatureDocuments { get; set; }
        public DbSet<ESignatureSigner> ESignatureSigners { get; set; }
        public DbSet<ESignatureSignature> ESignatureSignatures { get; set; }
        public DbSet<ESignatureAuditLog> ESignatureAuditLogs { get; set; }
        public DbSet<ESignatureNotification> ESignatureNotifications { get; set; }
        public DbSet<ESignatureAuthentication> ESignatureAuthentications { get; set; }
        public DbSet<ESignatureWorkflow> ESignatureWorkflows { get; set; }
        public DbSet<ESignatureWorkflowStep> ESignatureWorkflowSteps { get; set; }
        public DbSet<ESignatureTemplate> ESignatureTemplates { get; set; }
        public DbSet<ESignatureTemplateField> ESignatureTemplateFields { get; set; }
        
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<PaymentSplit> PaymentSplits { get; set; }
        public DbSet<PayoutAccount> PayoutAccounts { get; set; }
        public DbSet<PaymentLink> PaymentLinks { get; set; }
        public DbSet<ZatcaInvoice> ZatcaInvoices { get; set; }
        public DbSet<LoanApplication> LoanApplications { get; set; }
        public DbSet<PaymentQueue> PaymentQueues { get; set; }


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

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Apply soft delete
            ApplySoftDelete();
            
            var result = await base.SaveChangesAsync(cancellationToken);
            
            return result;
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


    }
}

