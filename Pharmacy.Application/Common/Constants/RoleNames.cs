namespace Pharmacy.Application.Common.Constants;

public static class RoleNames
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string Pharmacist = "Pharmacist";

    public static readonly IReadOnlyList<string> TenantRoles = [TenantAdmin, Pharmacist];
    public static readonly IReadOnlyList<string> PlatformRoles = [PlatformAdmin];
    public static readonly IReadOnlyList<string> All = [PlatformAdmin, TenantAdmin, Pharmacist];
}
