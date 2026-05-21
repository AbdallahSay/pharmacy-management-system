using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pharmacy.Infrastructure.configurations
{
    public class SaleConfiguration : IEntityTypeConfiguration<Sale>
    {
        public void Configure(EntityTypeBuilder<Sale> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasAlternateKey(s => new { s.TenantId, s.Id });

            builder.Property(s => s.TotalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.SaleDate)
                .IsRequired();

            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => new { s.TenantId, s.UserId })
                .HasPrincipalKey(u => new { u.TenantId, u.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.SaleItems)
                .WithOne(si => si.Sale)
                .HasForeignKey(si => new { si.TenantId, si.SaleId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
