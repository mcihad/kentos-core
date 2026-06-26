using Kentos.Infrastructure.Options;
using Kentos.Modules.Settlement.Application.Neighborhoods;
using Kentos.Modules.Settlement.Domain;
using Kentos.Modules.Settlement.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kentos.Modules.Settlement.Services;

/// <summary>Application service encapsulating neighborhood data + geometry operations.</summary>
public interface INeighborhoodService
{
    Task<NeighborhoodResponse> CreateAsync(CreateNeighborhoodCommand command, CancellationToken cancellationToken);
    Task<NeighborhoodResponse> UpdateAsync(UpdateNeighborhoodCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<NeighborhoodResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<NeighborhoodResponse>> ListAsync(ListNeighborhoodsQuery query, CancellationToken cancellationToken);
}

public sealed class NeighborhoodService(SettlementDbContext db, IMapper mapper, IOptions<GeoOptions> geo) : INeighborhoodService
{
    private int Srid => geo.Value.Srid;

    public async Task<NeighborhoodResponse> CreateAsync(CreateNeighborhoodCommand command, CancellationToken cancellationToken)
    {
        var district = await db.Districts.FirstOrDefaultAsync(d => d.Uuid == command.DistrictId, cancellationToken)
            ?? throw NotFoundException.For("İlçe", command.DistrictId);

        var neighborhood = new Neighborhood
        {
            Name = command.Name,
            District = district,
            PostalCode = command.PostalCode,
            Center = command.Latitude is { } lat && command.Longitude is { } lng ? GeometryParser.CreatePoint(lat, lng, Srid) : null,
            Boundary = string.IsNullOrWhiteSpace(command.BoundaryWkt) ? null : GeometryParser.ParseMultiPolygon(command.BoundaryWkt, Srid),
        };

        db.Neighborhoods.Add(neighborhood);
        await db.SaveChangesAsync(cancellationToken);
        return mapper.Map<NeighborhoodResponse>(neighborhood);
    }

    public async Task<NeighborhoodResponse> UpdateAsync(UpdateNeighborhoodCommand command, CancellationToken cancellationToken)
    {
        var neighborhood = await db.Neighborhoods.Include(n => n.District).FirstOrDefaultAsync(n => n.Uuid == command.Id, cancellationToken)
            ?? throw NotFoundException.For("Mahalle", command.Id);

        neighborhood.Name = command.Name;
        neighborhood.PostalCode = command.PostalCode;
        neighborhood.Center = command.Latitude is { } lat && command.Longitude is { } lng ? GeometryParser.CreatePoint(lat, lng, Srid) : null;
        neighborhood.Boundary = string.IsNullOrWhiteSpace(command.BoundaryWkt) ? null : GeometryParser.ParseMultiPolygon(command.BoundaryWkt, Srid);

        await db.SaveChangesAsync(cancellationToken);
        return mapper.Map<NeighborhoodResponse>(neighborhood);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var neighborhood = await db.Neighborhoods.FirstOrDefaultAsync(n => n.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Mahalle", id);

        db.Neighborhoods.Remove(neighborhood); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NeighborhoodResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var neighborhood = await db.Neighborhoods.Include(n => n.District).FirstOrDefaultAsync(n => n.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Mahalle", id);
        return mapper.Map<NeighborhoodResponse>(neighborhood);
    }

    public async Task<PagedResponse<NeighborhoodResponse>> ListAsync(ListNeighborhoodsQuery query, CancellationToken cancellationToken)
    {
        var source = db.Neighborhoods.AsNoTracking().Include(n => n.District).AsQueryable();

        if (query.DistrictId is { } districtId)
        {
            source = source.Where(n => n.District!.Uuid == districtId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(n => EF.Functions.ILike(n.Name, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(n => n.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<NeighborhoodResponse>(items.Select(mapper.Map<NeighborhoodResponse>).ToList(), total, query.Page, query.PageSize);
    }
}
