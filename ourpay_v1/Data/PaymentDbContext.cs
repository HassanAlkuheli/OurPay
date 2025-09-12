using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaymentApi.Models;

namespace PaymentApi.Data;

public class PaymentDbContext : IdentityDbContext<User, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public required DbSet<Payment> Payments { get; set; }
    public required DbSet<AuditLog> AuditLogs { get; set; }
    public required DbSet<Webhook> Webhooks { get; set; }
    public required DbSet<WebhookEvent> WebhookEvents { get; set; }
    public required DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)").HasDefaultValue(0.0m);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Index on email for faster lookups
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            
            // Use different default value based on database provider
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // SQLite doesn't support gen_random_uuid(), use application-generated GUIDs
                entity.Property(e => e.PaymentId).ValueGeneratedOnAdd();
            }
            else
            {
                // PostgreSQL
                entity.Property(e => e.PaymentId).HasDefaultValueSql("gen_random_uuid()");
            }
            
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Configure relationships
            entity.HasOne(p => p.Merchant)
                  .WithMany(u => u.MerchantPayments)
                  .HasForeignKey(p => p.MerchantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Customer)
                  .WithMany(u => u.CustomerPayments)
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            
            // Use different default value based on database provider
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // SQLite doesn't support gen_random_uuid(), use application-generated GUIDs
                entity.Property(e => e.LogId).ValueGeneratedOnAdd();
            }
            else
            {
                // PostgreSQL
                entity.Property(e => e.LogId).HasDefaultValueSql("gen_random_uuid()");
            }
            
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Details).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();

            // Configure relationships
            entity.HasOne(a => a.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure Webhook entity
        modelBuilder.Entity<Webhook>(entity =>
        {
            entity.HasKey(e => e.WebhookId);
            
            // Use different default value based on database provider
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // SQLite doesn't support gen_random_uuid(), use application-generated GUIDs
                entity.Property(e => e.WebhookId).ValueGeneratedOnAdd();
            }
            else
            {
                // PostgreSQL
                entity.Property(e => e.WebhookId).HasDefaultValueSql("gen_random_uuid()");
            }
            
            entity.Property(e => e.WebhookUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.EventTypes).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Configure relationships
            entity.HasOne(w => w.Merchant)
                  .WithMany()
                  .HasForeignKey(w => w.MerchantId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.IsActive);
        });

        // Configure WebhookEvent entity
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.WebhookEventId);
            
            // Use different default value based on database provider
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // SQLite doesn't support gen_random_uuid(), use application-generated GUIDs
                entity.Property(e => e.WebhookEventId).ValueGeneratedOnAdd();
            }
            else
            {
                // PostgreSQL
                entity.Property(e => e.WebhookEventId).HasDefaultValueSql("gen_random_uuid()");
            }
            
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();

            // Configure relationships
            entity.HasOne(we => we.Payment)
                  .WithMany()
                  .HasForeignKey(we => we.PaymentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure WebhookDeliveryAttempt entity
        modelBuilder.Entity<WebhookDeliveryAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId);
            
            // Use different default value based on database provider
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // SQLite doesn't support gen_random_uuid(), use application-generated GUIDs
                entity.Property(e => e.AttemptId).ValueGeneratedOnAdd();
            }
            else
            {
                // PostgreSQL
                entity.Property(e => e.AttemptId).HasDefaultValueSql("gen_random_uuid()");
            }
            
            entity.Property(e => e.StatusCode).IsRequired();
            entity.Property(e => e.AttemptedAt).IsRequired();

            // Configure relationships
            entity.HasOne(wda => wda.WebhookEvent)
                  .WithMany(we => we.DeliveryAttempts)
                  .HasForeignKey(wda => wda.WebhookEventId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.WebhookEventId);
            entity.HasIndex(e => e.AttemptedAt);
            entity.HasIndex(e => e.StatusCode);
        });

        // Configure enum conversions
        modelBuilder.Entity<User>()
            .Property(e => e.Role)
            .HasConversion<int>();

        modelBuilder.Entity<Payment>()
            .Property(e => e.Status)
            .HasConversion<int>();
    }
}
