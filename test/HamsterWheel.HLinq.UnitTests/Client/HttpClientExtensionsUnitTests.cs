using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.UnitTests.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Client;

public class HttpClientExtensionsUnitTests
{
    [Fact]
    public async Task GetWithHLinq_WhenServerResponseWithInvalidJson_ThenThrowsException()
    {
        //arrange
        var clientHandler = new DummyHttpClientHandler(new HttpResponseMessage { Content = new StringContent("null") });
        var client = new HttpClient(clientHandler);
        client.BaseAddress = new Uri("http://localhost");
        var action = () => client.GetWithHLinq("/demo", q => q.For<DummyEntity>());

        //act
        var actual = await action.Should().ThrowAsync<CouldNotDeserializeException<DummyEntity[]>>();

        //assert
        actual.WithMessage("Could not deserialize string*");
    }

    public class DummyHttpClientHandler(HttpResponseMessage response) : HttpClientHandler
    {
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response);
    }
}