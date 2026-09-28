namespace LeaveService.Models;

/// <summary>
/// Role names issued by IdentityService in the JWT role claim.
/// They are constants so they can be used inside attributes.
/// </summary>
public static class LeaveRoles
{
    public const string Student = "STUDENT";

    public const string Warden = "WARDEN";

    public const string HostelMaster = "HOSTEL_MASTER";

    public const string Admin = "ADMIN";

    // Used in the audit log when the system itself acts.
    public const string System = "SYSTEM";
}
