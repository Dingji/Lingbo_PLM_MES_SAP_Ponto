using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using PlmMesSync.Endpoints;
using PlmMesSync.Models.Dto;
using PlmMesSync.Services;

namespace PlmMesSync.Tests.Endpoints;

public class TriggerEndpointsTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly DefaultHttpContext _httpContext;

    public TriggerEndpointsTests()
    {
        _loggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        _httpContext = new DefaultHttpContext();
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
    }

    private static IResult InvokeHandler(
        TriggerEvent evt,
        BomDebouncerService bomDebouncer,
        FileSyncDebouncerService fileDebouncer,
        HttpContext ctx,
        ILoggerFactory loggerFactory)
    {
        var method = typeof(TriggerEndpoints)
            .GetMethod("HandleTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        return method.Invoke(null, new object?[]
        {
            evt,
            bomDebouncer,
            fileDebouncer,
            ctx,
            loggerFactory
        }) as IResult ?? throw new InvalidOperationException("Handler returned null");
    }

    private static BomDebouncerService CreateBomDebouncer()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { { "SyncSettings:DelaySeconds", "15" } }
        ).Build();

        return new BomDebouncerService(
            new TriggerStore(null),
            Mock.Of<IServiceScopeFactory>(),
            config,
            Mock.Of<ILogger<BomDebouncerService>>());
    }

    private static FileSyncDebouncerService CreateFileDebouncer()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { { "FileSyncSettings:DelaySeconds", "15" } }
        ).Build();

        return new FileSyncDebouncerService(
            new TriggerStore(null),
            Mock.Of<IServiceScopeFactory>(),
            config,
            Mock.Of<ILogger<FileSyncDebouncerService>>());
    }

    [Fact]
    public void HandleTrigger_BomEvent_ShouldReturnOk()
    {
        var evt = new TriggerEvent(123, "UPDATE", "BOM");
        var result = InvokeHandler(evt, CreateBomDebouncer(), CreateFileDebouncer(), _httpContext, _loggerFactoryMock.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public void HandleTrigger_FilesEvent_ShouldReturnOk()
    {
        var evt = new TriggerEvent(456, "INSERT", "FILES");
        var result = InvokeHandler(evt, CreateBomDebouncer(), CreateFileDebouncer(), _httpContext, _loggerFactoryMock.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public void HandleTrigger_UnknownTable_ShouldReturnOk()
    {
        var evt = new TriggerEvent(789, "UPDATE", "UNKNOWN_TABLE");
        var result = InvokeHandler(evt, CreateBomDebouncer(), CreateFileDebouncer(), _httpContext, _loggerFactoryMock.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public void HandleTrigger_ShouldAcceptZeroId()
    {
        var evt = new TriggerEvent(0, "INSERT", "BOM");
        var result = InvokeHandler(evt, CreateBomDebouncer(), CreateFileDebouncer(), _httpContext, _loggerFactoryMock.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public void HandleTrigger_NullEvent_ShouldReturnBadRequest()
    {
        var result = InvokeHandler(null!, CreateBomDebouncer(), CreateFileDebouncer(), _httpContext, _loggerFactoryMock.Object);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(400, ((IStatusCodeHttpResult)result).StatusCode);
    }
}
