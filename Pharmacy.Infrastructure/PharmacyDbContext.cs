using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Pharmacy.Infrastructure
{
    public class PharmacyDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure relationships and constraints here if needed
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }


    }
}
