using MongoDB.Driver;

namespace Kentos.Infrastructure.Auditing;

/// <summary>Writes audit entries to MongoDB (production target).</summary>
public sealed class MongoAuditWriter : IAuditWriter
{
    private readonly IMongoCollection<AuditLog> _collection;

    public MongoAuditWriter(IMongoDatabase database) =>
        _collection = database.GetCollection<AuditLog>("denetim_kayitlari");

    public async Task WriteAsync(IReadOnlyList<AuditLog> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return;
        }

        await _collection.InsertManyAsync(records, options: null, cancellationToken);
    }
}
