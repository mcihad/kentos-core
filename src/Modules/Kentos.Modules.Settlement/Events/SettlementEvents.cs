namespace Kentos.Modules.Settlement.Events;

// Domain events published through Wolverine after a successful write. Decoupled
// consumers (in this or another module) react to them. Cross-module events belong
// in SharedKernel; these are module-local for the reference example.

public sealed record ProvinceCreated(Guid Id, string Name);

public sealed record DistrictCreated(Guid Id, string Name, Guid ProvinceId);

public sealed record NeighborhoodCreated(Guid Id, string Name, Guid DistrictId);
