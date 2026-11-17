using AwesomeAssertions;
using BillerContracts.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using WebAPI.Middleware;

namespace xUnitTests.WebAPI.Middleware;

public class ErrorCheckingTest
{
    private readonly Mock<ILogger<ErrorChecking>> _loggerMock = new(MockBehavior.Strict);

    private static DefaultHttpContext NewContext() => new()
    {
        RequestServices = new ServiceCollection().BuildServiceProvider(),
        Response = { Body = new MemoryStream() }
    };

    private static ErrorResponse ReadResponse(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return JsonSerializer.Deserialize<ErrorResponse>(context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    [Fact]
    public async Task InvokeAsync_GivenValidationException_ReturnsBadRequestAndLogsWarningWithoutStackTrace()
    {
        //Arrange
        DefaultHttpContext context = NewContext();
        ErrorChecking middleware = new(_ => throw new ValidationException("Please use only characters the invoice PDF can show in Name. Can't show: 🔥 (U+1F525)"),
            _loggerMock.Object);

        _loggerMock.Setup(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        //Act
        await middleware.InvokeAsync(context);

        //Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ReadResponse(context).Should().BeEquivalentTo(new ErrorResponse
        {
            Message = "Validation failure",
            ExtendedMessage = "Please use only characters the invoice PDF can show in Name. Can't show: 🔥 (U+1F525)"
        });

        _loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAsync_GivenUnknownException_ReturnsGeneralErrorAndLogsErrorWithTheException()
    {
        //Arrange
        DefaultHttpContext context = NewContext();
        InvalidOperationException error = new("The Wall has fallen");
        ErrorChecking middleware = new(_ => throw error, _loggerMock.Object);

        _loggerMock.Setup(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), error, It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        //Act
        await middleware.InvokeAsync(context);

        //Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        ReadResponse(context).Should().BeEquivalentTo(new ErrorResponse
        {
            Message = "General error",
            ExtendedMessage = "The Wall has fallen"
        });

        _loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), error, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once());
    }
}
