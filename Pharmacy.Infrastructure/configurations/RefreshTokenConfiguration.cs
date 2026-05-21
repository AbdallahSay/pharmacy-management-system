using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Domain.Entities;

namespace Pharmacy.Infrastructure.configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(t => t.RevokedByIp)
            .HasMaxLength(50);
            
        builder.Property(t => t.UserAgent)
            .HasMaxLength(500);

        builder.Property(t => t.DeviceInfo)
            .HasMaxLength(256);

        builder.Property(t => t.ReasonRevoked)
            .HasMaxLength(256);

        // Indexes for performance and uniqueness
        builder.HasIndex(t => t.TokenHash).IsUnique();
        
        // Composite index to quickly fetch tokens for a given user and tenant
        builder.HasIndex(t => new { t.UserId, t.TenantId });
        
        // Index for background cleanup queries
        builder.HasIndex(t => new { t.RevokedAt, t.ExpiresAt });

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(t => t.ParentToken)
            .WithMany(t => t.ChildTokens)
            .HasForeignKey(t => t.ParentTokenId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
