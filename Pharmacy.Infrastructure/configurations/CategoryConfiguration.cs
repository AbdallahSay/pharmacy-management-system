using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Pharmacy.Infrastructure.configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);

            builder.HasAlternateKey(c => new { c.TenantId, c.Id });

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasMany(c => c.Medicines)
                .WithOne(m => m.Category)
                .HasForeignKey(m => new { m.TenantId, m.CategoryId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.TenantId, c.Name })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
