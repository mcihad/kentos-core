using Kentos.SharedKernel.Authorization;

namespace Kentos.Modules.Settlement.Permissions;

/// <summary>All permission keys + definitions declared by the Settlement module.</summary>
public static class SettlementPermissions
{
    public const string ModuleSlug = "settlement";

    public static class Province
    {
        public const string List = "settlement.province.list";
        public const string View = "settlement.province.view";
        public const string Create = "settlement.province.create";
    }

    public static class District
    {
        public const string List = "settlement.district.list";
        public const string View = "settlement.district.view";
        public const string Create = "settlement.district.create";
    }

    public static class Neighborhood
    {
        public const string List = "settlement.neighborhood.list";
        public const string View = "settlement.neighborhood.view";
        public const string Create = "settlement.neighborhood.create";
        public const string Update = "settlement.neighborhood.update";
        public const string Delete = "settlement.neighborhood.delete";
    }

    // Permission keys are English; titles are Turkish display labels.
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        Def("province", PermissionAction.List, "İlleri listele"),
        Def("province", PermissionAction.View, "İl görüntüle"),
        Def("province", PermissionAction.Create, "İl ekle"),
        Def("district", PermissionAction.List, "İlçeleri listele"),
        Def("district", PermissionAction.View, "İlçe görüntüle"),
        Def("district", PermissionAction.Create, "İlçe ekle"),
        Def("neighborhood", PermissionAction.List, "Mahalleleri listele"),
        Def("neighborhood", PermissionAction.View, "Mahalle görüntüle"),
        Def("neighborhood", PermissionAction.Create, "Mahalle ekle"),
        Def("neighborhood", PermissionAction.Update, "Mahalle güncelle"),
        Def("neighborhood", PermissionAction.Delete, "Mahalle sil"),
    ];

    private static PermissionDefinition Def(string resource, string action, string title) =>
        PermissionDefinition.Create(ModuleSlug, resource, action, title);
}
