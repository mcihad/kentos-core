using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kentos.Modules.Hesap.Application.Roles;
using Kentos.Modules.Hesap.Permissions;
using Kentos.SharedKernel.Pagination;
using Kentos.TestShared;
using Shouldly;

namespace Kentos.Modules.Hesap.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HesapRoleTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int _seq;

    private readonly ApiFactory _factory;

    public HesapRoleTests(ApiFactory factory) => _factory = factory;

    private static string NextName() => $"rol-{Interlocked.Increment(ref _seq)}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Create_role_then_assign_permission_and_read_back()
    {
        var client = _factory.CreateClientWith(
            HesapPermissions.Role.Create, HesapPermissions.Role.View, HesapPermissions.Role.AssignPermissions);

        var create = await client.PostAsJsonAsync(
            "/api/v1/hesap/roles", new { name = NextName(), description = "test" }, Json);
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var role = await create.Content.ReadFromJsonAsync<RoleResponse>(Json);
        role!.Id.ShouldNotBe(Guid.Empty);
        role.PermissionCount.ShouldBe(0);

        var assign = await client.PutAsJsonAsync(
            $"/api/v1/hesap/roles/{role.Id}/permissions",
            new { permissionKeys = new[] { HesapPermissions.Role.List } },
            Json);
        assign.StatusCode.ShouldBe(HttpStatusCode.OK);

        var detail = await client.GetFromJsonAsync<RoleDetailResponse>($"/api/v1/hesap/roles/{role.Id}", Json);
        detail!.Permissions.ShouldContain(HesapPermissions.Role.List);
    }

    [Fact]
    public async Task Create_role_with_empty_name_fails_validation_via_wolverine()
    {
        var client = _factory.CreateClientWith(HesapPermissions.Role.Create);

        var response = await client.PostAsJsonAsync("/api/v1/hesap/roles", new { name = "", description = "x" }, Json);

        // Wolverine's FluentValidation middleware → 400 ProblemDetails.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assign_unknown_permission_is_business_rule_error()
    {
        var client = _factory.CreateClientWith(
            HesapPermissions.Role.Create, HesapPermissions.Role.AssignPermissions);

        var role = await (await client.PostAsJsonAsync(
            "/api/v1/hesap/roles", new { name = NextName() }, Json)).Content.ReadFromJsonAsync<RoleResponse>(Json);

        var assign = await client.PutAsJsonAsync(
            $"/api/v1/hesap/roles/{role!.Id}/permissions",
            new { permissionKeys = new[] { "does.not.exist" } },
            Json);

        assign.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity); // BusinessRuleException → 422
    }
}
