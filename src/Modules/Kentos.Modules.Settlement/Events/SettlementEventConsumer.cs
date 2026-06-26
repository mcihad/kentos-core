using Microsoft.Extensions.Logging;

namespace Kentos.Modules.Settlement.Events;

/// <summary>
/// Wolverine consumer reacting to Settlement domain events. The type name ends in
/// "Consumer" so Wolverine discovers its Handle methods. This is what justifies
/// Wolverine: writes publish an event and decoupled consumers (here logging; in a real
/// system: read-model updates, cache invalidation, notifications, other modules) react
/// without the producer knowing about them.
/// </summary>
public static class SettlementEventConsumer
{
    public static void Handle(ProvinceCreated message, ILogger<ProvinceCreated> logger) =>
        logger.LogInformation("[event] İl oluşturuldu: {Name} ({Id})", message.Name, message.Id);

    public static void Handle(DistrictCreated message, ILogger<DistrictCreated> logger) =>
        logger.LogInformation("[event] İlçe oluşturuldu: {Name} ({Id})", message.Name, message.Id);

    public static void Handle(NeighborhoodCreated message, ILogger<NeighborhoodCreated> logger) =>
        logger.LogInformation("[event] Mahalle oluşturuldu: {Name} ({Id})", message.Name, message.Id);
}
