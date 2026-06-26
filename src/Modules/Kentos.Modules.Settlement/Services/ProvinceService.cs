using Kentos.Modules.Settlement.Application.Provinces;
using Kentos.Modules.Settlement.Domain;
using Kentos.Modules.Settlement.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Settlement.Services;

/// <summary>Application service encapsulating province data operations.</summary>
public interface IProvinceService
{
    Task<ProvinceResponse> CreateAsync(CreateProvinceCommand command, CancellationToken cancellationToken);
    Task<ProvinceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<ProvinceResponse>> ListAsync(ListProvincesQuery query, CancellationToken cancellationToken);
}

public sealed class ProvinceService(SettlementDbContext db, IMapper mapper) : IProvinceService
{
    public async Task<ProvinceResponse> CreateAsync(CreateProvinceCommand command, CancellationToken cancellationToken)
    {
        var province = new Province { Name = command.Name, PlateCode = command.PlateCode };
        db.Provinces.Add(province);
        await db.SaveChangesAsync(cancellationToken);
        return mapper.Map<ProvinceResponse>(province);
    }

    public async Task<ProvinceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var province = await db.Provinces.FirstOrDefaultAsync(p => p.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("İl", id);
        return mapper.Map<ProvinceResponse>(province);
    }

    public async Task<PagedResponse<ProvinceResponse>> ListAsync(ListProvincesQuery query, CancellationToken cancellationToken)
    {
        var source = db.Provinces.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(p => EF.Functions.ILike(p.Name, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(p => p.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProvinceResponse>(items.Select(mapper.Map<ProvinceResponse>).ToList(), total, query.Page, query.PageSize);
    }
}
