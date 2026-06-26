using Kentos.Modules.Settlement.Application.Districts;
using Kentos.Modules.Settlement.Domain;
using Kentos.Modules.Settlement.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Settlement.Services;

/// <summary>Application service encapsulating district data operations.</summary>
public interface IDistrictService
{
    Task<DistrictResponse> CreateAsync(CreateDistrictCommand command, CancellationToken cancellationToken);
    Task<DistrictResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<DistrictResponse>> ListAsync(ListDistrictsQuery query, CancellationToken cancellationToken);
}

public sealed class DistrictService(SettlementDbContext db, IMapper mapper) : IDistrictService
{
    public async Task<DistrictResponse> CreateAsync(CreateDistrictCommand command, CancellationToken cancellationToken)
    {
        var province = await db.Provinces.FirstOrDefaultAsync(p => p.Uuid == command.ProvinceId, cancellationToken)
            ?? throw NotFoundException.For("İl", command.ProvinceId);

        var district = new District { Name = command.Name, Province = province };
        db.Districts.Add(district);
        await db.SaveChangesAsync(cancellationToken);
        return mapper.Map<DistrictResponse>(district);
    }

    public async Task<DistrictResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var district = await db.Districts.Include(d => d.Province).FirstOrDefaultAsync(d => d.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("İlçe", id);
        return mapper.Map<DistrictResponse>(district);
    }

    public async Task<PagedResponse<DistrictResponse>> ListAsync(ListDistrictsQuery query, CancellationToken cancellationToken)
    {
        var source = db.Districts.AsNoTracking().Include(d => d.Province).AsQueryable();

        if (query.ProvinceId is { } provinceId)
        {
            source = source.Where(d => d.Province!.Uuid == provinceId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(d => EF.Functions.ILike(d.Name, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(d => d.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<DistrictResponse>(items.Select(mapper.Map<DistrictResponse>).ToList(), total, query.Page, query.PageSize);
    }
}
