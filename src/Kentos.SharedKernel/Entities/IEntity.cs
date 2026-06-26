namespace Kentos.SharedKernel.Entities;

/// <summary>Identity contract shared by all persisted entities.</summary>
public interface IEntity
{
    /// <summary>Internal numeric primary key (DB joins/performance; never exposed by the API).</summary>
    long Id { get; }

    /// <summary>Public UUIDv7 identity; exposed as "id" by the API.</summary>
    Guid Uuid { get; }
}
