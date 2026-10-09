using Clients.Clients;
using Domain.Exceptions;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace xUnitTests.Clients;

public class BaseHttpClientTest
{
    private const string _urlBase = "https://api.test";
    private const string _endpoint = "items";

    public record TestData(int Id, string Name);

    private class TestHttpClient(IHttpClientFactory factory, string urlBase) : BaseHttpClient(factory, urlBase)
    {
        public Task<T> Get<T>(Dictionary<string, string> query, Dictionary<string, string> headers) =>
            GetAsync<T>(_endpoint, query, headers);

        public Task<T> Post<T>(Dictionary<string, string> query, Dictionary<string, string> headers, T data) =>
            PostAsync(_endpoint, query, headers, data);

        public Task<T> Put<T>(Dictionary<string, string> query, Dictionary<string, string> headers, T data) =>
            PutAsync(_endpoint, query, headers, data);
    }

    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly Mock<IHttpClientFactory> _factoryMock;
    private readonly TestHttpClient _client;
    private HttpRequestMessage? _sentRequest;
    private string? _sentBody;

    public BaseHttpClientTest()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        _factoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
                    .Returns(() => new HttpClient(_handlerMock.Object));

        _client = new TestHttpClient(_factoryMock.Object, _urlBase);
    }

    private void SetupResponse(HttpStatusCode statusCode, string body)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                _sentRequest = request;
                _sentBody = request.Content?.ReadAsStringAsync().Result;
            })
            .ReturnsAsync(() => new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
    }

    private void VerifySentOnce()
    {
        _handlerMock.Protected()
            .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        _factoryMock.Verify(m => m.CreateClient(It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void GenerateUrl_GivenNoQueryParameters_ReturnsBaseWithEndpoint()
    {
        //Arrange
        var client = new BaseHttpClient(_factoryMock.Object, _urlBase);

        //Act
        Uri result = client.GenerateUrl(_endpoint);

        //Assert
        result.Should().Be(new Uri($"{_urlBase}/{_endpoint}"));
    }

    [Fact]
    public void GenerateUrl_GivenEmptyQueryParameters_ReturnsBaseWithEndpoint()
    {
        //Arrange
        var client = new BaseHttpClient(_factoryMock.Object, _urlBase);

        //Act
        Uri result = client.GenerateUrl(_endpoint, []);

        //Assert
        result.Should().Be(new Uri($"{_urlBase}/{_endpoint}"));
    }

    [Fact]
    public void GenerateUrl_GivenQueryParameters_AppendsEscapedQuery()
    {
        //Arrange
        var client = new BaseHttpClient(_factoryMock.Object, _urlBase);
        var query = new Dictionary<string, string> { ["name"] = "a b&c", ["page"] = "2" };

        //Act
        Uri result = client.GenerateUrl(_endpoint, query);

        //Assert
        result.AbsoluteUri.Should().Be($"{_urlBase}/{_endpoint}?name=a%20b%26c&page=2");
    }

    [Fact]
    public void AddHeaders_GivenHeaders_AddsAllToRequest()
    {
        //Arrange
        var request = new HttpRequestMessage();
        var headers = new Dictionary<string, string> { ["X-Api-Key"] = "secret", ["X-Trace"] = "1" };

        //Act
        BaseHttpClient.AddHeaders(request, headers);

        //Assert
        request.Headers.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be("secret");
        request.Headers.GetValues("X-Trace").Should().ContainSingle().Which.Should().Be("1");
    }

    [Fact]
    public void AddHeaders_GivenNullHeaders_AddsNothing()
    {
        //Arrange
        var request = new HttpRequestMessage();

        //Act
        BaseHttpClient.AddHeaders(request, null);

        //Assert
        request.Headers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_GivenSuccessResponse_ReturnsDeserializedData()
    {
        //Arrange
        var expected = new TestData(1, "item");
        SetupResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(expected));
        var query = new Dictionary<string, string> { ["id"] = "1" };
        var headers = new Dictionary<string, string> { ["X-Api-Key"] = "secret" };

        //Act
        TestData result = await _client.Get<TestData>(query, headers);

        //Assert
        result.Should().BeEquivalentTo(expected);
        _sentRequest!.Method.Should().Be(HttpMethod.Get);
        _sentRequest.RequestUri!.AbsoluteUri.Should().Be($"{_urlBase}/{_endpoint}?id=1");
        _sentRequest.Headers.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be("secret");

        VerifySentOnce();
    }

    [Fact]
    public async Task PostAsync_GivenSuccessResponse_SendsJsonAndReturnsData()
    {
        //Arrange
        var data = new TestData(2, "posted");
        SetupResponse(HttpStatusCode.Created, JsonConvert.SerializeObject(data));

        //Act
        TestData result = await _client.Post(new Dictionary<string, string>(), new Dictionary<string, string>(), data);

        //Assert
        result.Should().BeEquivalentTo(data);
        _sentRequest!.Method.Should().Be(HttpMethod.Post);
        _sentRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        JsonConvert.DeserializeObject<TestData>(_sentBody!).Should().BeEquivalentTo(data);

        VerifySentOnce();
    }

    [Fact]
    public async Task PutAsync_GivenSuccessResponse_SendsJsonAndReturnsData()
    {
        //Arrange
        var data = new TestData(3, "put");
        SetupResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(data));

        //Act
        TestData result = await _client.Put(new Dictionary<string, string>(), new Dictionary<string, string>(), data);

        //Assert
        result.Should().BeEquivalentTo(data);
        _sentRequest!.Method.Should().Be(HttpMethod.Put);
        _sentRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        JsonConvert.DeserializeObject<TestData>(_sentBody!).Should().BeEquivalentTo(data);

        VerifySentOnce();
    }

    [Fact]
    public async Task GetAsync_GivenErrorResponseWithMessage_ThrowsWithMessage()
    {
        //Arrange
        SetupResponse(HttpStatusCode.BadRequest, "{\"Message\":\"bad input\"}");

        //Act
        Func<Task> act = () => _client.Get<TestData>([], []);

        //Assert
        await act.Should().ThrowAsync<ClientAPIException>()
            .WithMessage($"Failed to get data from client {_urlBase}, error code: BadRequest, with message: bad input");

        VerifySentOnce();
    }

    [Fact]
    public async Task GetAsync_GivenErrorResponseWithEmptyBody_ThrowsWithBody()
    {
        //Arrange
        SetupResponse(HttpStatusCode.InternalServerError, "");

        //Act
        Func<Task> act = () => _client.Get<TestData>([], []);

        //Assert
        await act.Should().ThrowAsync<ClientAPIException>()
            .WithMessage($"Failed to get data from client {_urlBase}, error code: InternalServerError, with body: ");

        VerifySentOnce();
    }

    [Fact]
    public async Task GetAsync_GivenErrorResponseWithInvalidJson_ThrowsWithBody()
    {
        //Arrange
        SetupResponse(HttpStatusCode.BadGateway, "<html>oops</html>");

        //Act
        Func<Task> act = () => _client.Get<TestData>([], []);

        //Assert
        await act.Should().ThrowAsync<ClientAPIException>()
            .WithMessage($"Failed to get data from client {_urlBase}, error code: BadGateway, with body: <html>oops</html>");

        VerifySentOnce();
    }

    [Fact]
    public async Task PostAsync_GivenErrorResponse_ThrowsClientApiException()
    {
        //Arrange
        SetupResponse(HttpStatusCode.Conflict, "{\"Message\":\"exists\"}");

        //Act
        Func<Task> act = () => _client.Post([], [], new TestData(1, "x"));

        //Assert
        await act.Should().ThrowAsync<ClientAPIException>().WithMessage("*Conflict, with message: exists");

        VerifySentOnce();
    }

    [Fact]
    public async Task PutAsync_GivenErrorResponse_ThrowsClientApiException()
    {
        //Arrange
        SetupResponse(HttpStatusCode.NotFound, "{\"Message\":\"missing\"}");

        //Act
        Func<Task> act = () => _client.Put([], [], new TestData(1, "x"));

        //Assert
        await act.Should().ThrowAsync<ClientAPIException>().WithMessage("*NotFound, with message: missing");

        VerifySentOnce();
    }

    [Fact]
    public async Task GetAsync_GivenNullBody_ThrowsDeserializeException()
    {
        //Arrange
        SetupResponse(HttpStatusCode.OK, "null");

        //Act
        Func<Task> act = () => _client.Get<TestData>([], []);

        //Assert
        await act.Should().ThrowAsync<ClientAPIException>()
            .WithMessage($"Failed to deserialize data from client {_urlBase}, with body: null");

        VerifySentOnce();
    }
}
