using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

[CollectionDefinition(nameof(IntegrationCollectionDefinition))]
public class IntegrationCollectionDefinition : ICollectionFixture<DemoFixture>;