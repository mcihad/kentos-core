using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kentos.Modules.Settlement.Application.Districts;
using Kentos.Modules.Settlement.Application.Neighborhoods;
using Kentos.Modules.Settlement.Application.Provinces;
using Kentos.Modules.Settlement.Permissions;
using Kentos.SharedKernel.Pagination;
using Kentos.TestShared;
using Shouldly;

namespace Kentos.Modules.Settlement.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class SettlementApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int _plate;

    private readonly ApiFactory _factory;

    public SettlementApiTests(ApiFactory factory) => _factory = factory;

    private static int NextPlate() => (Interlocked.Increment(ref _plate) % 81) + 1;

    [Fact]
    public async Task Anonymous_request_is_unauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/settlement/provinces");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_without_permission_is_forbidden()
    {
        var response = await _factory.CreateClientWith().GetAsync("/api/v1/settlement/provinces");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_and_get_province_succeeds_with_permission()
    {
        var client = _factory.CreateClientWith(SettlementPermissions.Province.Create, SettlementPermissions.Province.View);

        var created = await CreateProvince(client);
        created.Id.ShouldNotBe(Guid.Empty);

        var fetched = await client.GetFromJsonAsync<ProvinceResponse>($"/api/v1/settlement/provinces/{created.Id}", Json);
        fetched!.Name.ShouldBe(created.Name);
    }

    [Fact]
    public async Task Create_hierarchy_and_neighborhood_persists_geometry()
    {
        var client = _factory.CreateClientWith(
            SettlementPermissions.Province.Create,
            SettlementPermissions.District.Create,
            SettlementPermissions.Neighborhood.Create,
            SettlementPermissions.Neighborhood.View);

        var province = await CreateProvince(client);
        var district = await CreateDistrict(client, province.Id);

        var createResponse = await client.PostAsJsonAsync("/api/v1/settlement/neighborhoods", new
        {
            name = $"N-{Guid.NewGuid():N}"[..12],
            districtId = district.Id,
            postalCode = "34710",
            latitude = 40.98,
            longitude = 29.03,
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var neighborhood = (await createResponse.Content.ReadFromJsonAsync<NeighborhoodResponse>(Json))!;

        var fetched = await client.GetFromJsonAsync<NeighborhoodResponse>($"/api/v1/settlement/neighborhoods/{neighborhood.Id}", Json);
        fetched!.DistrictId.ShouldBe(district.Id);
        fetched.Latitude!.Value.ShouldBe(40.98, 0.0001);
        fetched.Longitude!.Value.ShouldBe(29.03, 0.0001);
    }

    [Fact]
    public async Task Delete_neighborhood_soft_deletes_it()
    {
        var client = _factory.CreateClientWith(
            SettlementPermissions.Province.Create,
            SettlementPermissions.District.Create,
            SettlementPermissions.Neighborhood.Create,
            SettlementPermissions.Neighborhood.View,
            SettlementPermissions.Neighborhood.Delete);

        var province = await CreateProvince(client);
        var district = await CreateDistrict(client, province.Id);
        var create = await client.PostAsJsonAsync("/api/v1/settlement/neighborhoods", new
        {
            name = $"N-{Guid.NewGuid():N}"[..12],
            districtId = district.Id,
        });
        var neighborhood = (await create.Content.ReadFromJsonAsync<NeighborhoodResponse>(Json))!;

        var delete = await client.DeleteAsync($"/api/v1/settlement/neighborhoods/{neighborhood.Id}");
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterDelete = await client.GetAsync($"/api/v1/settlement/neighborhoods/{neighborhood.Id}");
        afterDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_provinces_is_paginated()
    {
        var client = _factory.CreateClientWith(SettlementPermissions.Province.Create, SettlementPermissions.Province.List);
        await CreateProvince(client);
        await CreateProvince(client);

        var page = await client.GetFromJsonAsync<PagedResponse<ProvinceResponse>>("/api/v1/settlement/provinces?page=1&pageSize=1", Json);
        page!.PageSize.ShouldBe(1);
        page.Items.Count.ShouldBe(1);
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    private static async Task<ProvinceResponse> CreateProvince(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/settlement/provinces", new
        {
            name = $"P-{Guid.NewGuid():N}"[..12],
            plateCode = NextPlate(),
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, body);
        return (await response.Content.ReadFromJsonAsync<ProvinceResponse>(Json))!;
    }

    private static async Task<DistrictResponse> CreateDistrict(HttpClient client, Guid provinceId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/settlement/districts", new
        {
            name = $"D-{Guid.NewGuid():N}"[..12],
            provinceId,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<DistrictResponse>(Json))!;
    }
}
