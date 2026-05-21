using Microsoft.AspNetCore.Identity;
namespace Pharmacy.Domain.Entities
{
    public class ApplicationUser : IdentityUser<int>, ITenantEntity
    {
        public int TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
