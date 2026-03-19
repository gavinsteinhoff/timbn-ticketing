namespace TimbnTicketing.Core;

[Flags]
public enum Permission : long
{
    None                  = 0,
    CanManageOrganization = 1L << 0,
    CanCreateEvents       = 1L << 1,
    CanManageEvents       = 1L << 2,
    CanManageRoles        = 1L << 3,
    CanManageBilling      = 1L << 4,
    CanCheckin            = 1L << 5,
    CanViewAttendees      = 1L << 6,
}
