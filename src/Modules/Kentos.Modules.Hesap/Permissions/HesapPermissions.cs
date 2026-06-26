using Kentos.SharedKernel.Authorization;

namespace Kentos.Modules.Hesap.Permissions;

/// <summary>All permission keys + definitions declared by the Hesap (Account) module.</summary>
public static class HesapPermissions
{
    public const string ModuleSlug = "hesap";

    public static class User
    {
        public const string List = "hesap.user.list";
        public const string View = "hesap.user.view";
        public const string Create = "hesap.user.create";
        public const string Update = "hesap.user.update";
        public const string Delete = "hesap.user.delete";
        public const string AssignRoles = "hesap.user.assignroles";
    }

    public static class Role
    {
        public const string List = "hesap.role.list";
        public const string View = "hesap.role.view";
        public const string Create = "hesap.role.create";
        public const string Update = "hesap.role.update";
        public const string Delete = "hesap.role.delete";
        public const string AssignPermissions = "hesap.role.assignpermissions";
    }

    public static class Permission
    {
        public const string List = "hesap.permission.list";
    }

    public static class Department
    {
        public const string List = "hesap.department.list";
        public const string View = "hesap.department.view";
        public const string Create = "hesap.department.create";
        public const string Update = "hesap.department.update";
        public const string Delete = "hesap.department.delete";
    }

    public static class Group
    {
        public const string List = "hesap.group.list";
        public const string View = "hesap.group.view";
        public const string Create = "hesap.group.create";
        public const string Update = "hesap.group.update";
        public const string Delete = "hesap.group.delete";
    }

    public static class Policy
    {
        public const string List = "hesap.policy.list";
        public const string Create = "hesap.policy.create";
        public const string Delete = "hesap.policy.delete";
    }

    // Permission keys are English; titles are Turkish display labels.
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        Def("user", PermissionAction.List, "Kullanıcıları listele"),
        Def("user", PermissionAction.View, "Kullanıcı görüntüle"),
        Def("user", PermissionAction.Create, "Kullanıcı ekle"),
        Def("user", PermissionAction.Update, "Kullanıcı güncelle"),
        Def("user", PermissionAction.Delete, "Kullanıcı sil"),
        Def("user", "assignroles", "Kullanıcıya rol ata"),

        Def("role", PermissionAction.List, "Rolleri listele"),
        Def("role", PermissionAction.View, "Rol görüntüle"),
        Def("role", PermissionAction.Create, "Rol ekle"),
        Def("role", PermissionAction.Update, "Rol güncelle"),
        Def("role", PermissionAction.Delete, "Rol sil"),
        Def("role", "assignpermissions", "Role yetki ata"),

        Def("permission", PermissionAction.List, "Yetkileri listele"),

        Def("department", PermissionAction.List, "Departmanları listele"),
        Def("department", PermissionAction.View, "Departman görüntüle"),
        Def("department", PermissionAction.Create, "Departman ekle"),
        Def("department", PermissionAction.Update, "Departman güncelle"),
        Def("department", PermissionAction.Delete, "Departman sil"),

        Def("group", PermissionAction.List, "Grupları listele"),
        Def("group", PermissionAction.View, "Grup görüntüle"),
        Def("group", PermissionAction.Create, "Grup ekle"),
        Def("group", PermissionAction.Update, "Grup güncelle"),
        Def("group", PermissionAction.Delete, "Grup sil"),

        Def("policy", PermissionAction.List, "Erişim politikalarını listele"),
        Def("policy", PermissionAction.Create, "Erişim politikası ekle"),
        Def("policy", PermissionAction.Delete, "Erişim politikası sil"),
    ];

    private static PermissionDefinition Def(string resource, string action, string title) =>
        PermissionDefinition.Create(ModuleSlug, resource, action, title);
}
