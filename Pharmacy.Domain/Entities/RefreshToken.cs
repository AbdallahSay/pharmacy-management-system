using Pharmacy.Domain.Entities;

namespace Pharmacy.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string TokenHash { get; set; } = string.Empty;
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int TenantId { get; set; }
    
    // Token Family Tracking (Chaining)
    public Guid FamilyId { get; set; }
    public int? ParentTokenId { get; set; }
    public RefreshToken? ParentToken { get; set; }
    public ICollection<RefreshToken> ChildTokens { get; set; } = new List<RefreshToken>();

    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    // Audit Fields
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    public string? ReasonRevoked { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
