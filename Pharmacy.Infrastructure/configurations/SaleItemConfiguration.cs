using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pharmacy.Infrastructure.configurations
{
    public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
    {
        public void Configure(EntityTypeBuilder<SaleItem> builder)
        {
            builder.HasKey(si => si.Id);

            builder.Property(si => si.UnitPrice)
                   .HasColumnType("decimal(18,2)");

            builder.Property(si => si.Quantity)
                   .IsRequired();

            builder.HasOne(si => si.Medicine)
                   .WithMany()
                   .HasForeignKey(si => si.MedicineId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
