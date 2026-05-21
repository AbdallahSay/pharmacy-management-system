using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Domain.Entities;

namespace Pharmacy.Infrastructure.configurations;

public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.HasKey(tu => tu.Id);

        builder.Property(tu => tu.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(tu => new { tu.TenantId, tu.UserId })
            .IsUnique();

        builder.HasOne(tu => tu.Tenant)
            .WithMany(t => t.TenantUsers)
            .HasForeignKey(tu => tu.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tu => tu.User)
            .WithMany()
            .HasForeignKey(tu => new { tu.TenantId, tu.UserId })
            .HasPrincipalKey(u => new { u.TenantId, u.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
