using Microsoft.Extensions.Logging;

namespace Kentos.Modules.Settlement.Events;

// Wolverine consumers reacting to Settlement domain events. One handler class per event
// (instance class, ends "Handler", single Handle) so Wolverine discovers and routes them.
// This is what justifies Wolverine: writes publish an event and decoupled consumers react —
// here logging; in a real system: read-model updates, cache invalidation, notifications, or
// another module — without the producer knowing about them.

public sealed class ProvinceCreatedHandler(ILogger<ProvinceCreatedHandler> logger)
{
    public void Handle(ProvinceCreated message) =>
        logger.LogInformation("[event] İl oluşturuldu: {Name} ({Id})", message.Name, message.Id);
}

public sealed class DistrictCreatedHandler(ILogger<DistrictCreatedHandler> logger)
{
    public void Handle(DistrictCreated message) =>
        logger.LogInformation("[event] İlçe oluşturuldu: {Name} ({Id})", message.Name, message.Id);
}

public sealed class NeighborhoodCreatedHandler(ILogger<NeighborhoodCreatedHandler> logger)
{
    public void Handle(NeighborhoodCreated message) =>
        logger.LogInformation("[event] Mahalle oluşturuldu: {Name} ({Id})", message.Name, message.Id);
}
