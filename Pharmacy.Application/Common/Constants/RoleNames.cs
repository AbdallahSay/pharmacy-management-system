namespace Pharmacy.Application.Common.Constants;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Pharmacist = "Pharmacist";

    public static readonly IReadOnlyList<string> All = [Admin, Pharmacist];
}
