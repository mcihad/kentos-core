using Kentos.TestShared;

namespace Kentos.Api.IntegrationTests;

/// <summary>Shares a single <see cref="ApiFactory"/> (one PostGIS container) across this assembly's tests.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
