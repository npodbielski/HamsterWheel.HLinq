using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

[Collection(nameof(IntegrationCollectionDefinition))]
public partial class MemoryDataTests(DemoFixture fixture);