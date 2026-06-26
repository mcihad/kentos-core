using Microsoft.Extensions.Logging;

namespace Kentos.Modules.Hesap.Events;

// Wolverine consumers reacting to Hesap domain events. One instance class per event,
// ending in "Handler" with a single Handle, so Wolverine discovers and routes them.
// Here they log; in a real system: read-model updates, cache invalidation, notifications,
// or another module reacting — without the producer referencing them.

public sealed class UserCreatedHandler(ILogger<UserCreatedHandler> logger)
{
    public void Handle(UserCreated message) =>
        logger.LogInformation("[event] Kullanıcı oluşturuldu: {UserName} ({Id})", message.UserName, message.Id);
}

public sealed class UserRolesAssignedHandler(ILogger<UserRolesAssignedHandler> logger)
{
    public void Handle(UserRolesAssigned message) =>
        logger.LogInformation("[event] Kullanıcı rolleri güncellendi: {Id} → {Roles}", message.UserId, string.Join(", ", message.Roles));
}

public sealed class RoleCreatedHandler(ILogger<RoleCreatedHandler> logger)
{
    public void Handle(RoleCreated message) =>
        logger.LogInformation("[event] Rol oluşturuldu: {Name} ({Id})", message.Name, message.Id);
}

public sealed class RolePermissionsAssignedHandler(ILogger<RolePermissionsAssignedHandler> logger)
{
    public void Handle(RolePermissionsAssigned message) =>
        logger.LogInformation("[event] Rol yetkileri güncellendi: {RoleId} ({Count} yetki)", message.RoleId, message.PermissionCount);
}

public sealed class DepartmentCreatedHandler(ILogger<DepartmentCreatedHandler> logger)
{
    public void Handle(DepartmentCreated message) =>
        logger.LogInformation("[event] Departman oluşturuldu: {Name} ({Id})", message.Name, message.Id);
}

public sealed class UserGroupCreatedHandler(ILogger<UserGroupCreatedHandler> logger)
{
    public void Handle(UserGroupCreated message) =>
        logger.LogInformation("[event] Grup oluşturuldu: {Name} ({Id})", message.Name, message.Id);
}

public sealed class AccessPolicyCreatedHandler(ILogger<AccessPolicyCreatedHandler> logger)
{
    public void Handle(AccessPolicyCreated message) =>
        logger.LogInformation("[event] Erişim politikası oluşturuldu: {Id}", message.Id);
}
