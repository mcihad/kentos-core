namespace Kentos.Modules.Hesap.Events;

// Domain events published through Wolverine after a successful write. Decoupled
// consumers (in this or another module) react to them without the producer knowing.
// Cross-module events belong in SharedKernel; these are module-local.

public sealed record UserCreated(Guid Id, string UserName);

public sealed record UserRolesAssigned(Guid UserId, IReadOnlyList<string> Roles);

public sealed record RoleCreated(Guid Id, string Name);

public sealed record RolePermissionsAssigned(Guid RoleId, int PermissionCount);

public sealed record DepartmentCreated(Guid Id, string Name);

public sealed record UserGroupCreated(Guid Id, string Name);

public sealed record AccessPolicyCreated(Guid Id);
