// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.given;

public class a_running_endpoint_mapper : Specification, IAsyncDisposable
{
    protected HttpListenerEndpointMapper _endpointMapper;
    protected HttpClient _httpClient;
    protected IServiceProvider _serviceProvider;
    protected string _testDirectory;
    protected int _port;

    /// <summary>
    /// Gets the host the listener binds and the client calls, on the same address family the port probe checked.
    /// </summary>
    /// <remarks>
    /// Off Windows, .NET's managed HttpListener binds a <c>localhost</c> prefix to only the first address
    /// <c>localhost</c> resolves to - often <c>::1</c> - while the port was probed free on <c>127.0.0.1</c>, and the
    /// client resolves <c>localhost</c> on its own. The three could land on different address families, which showed up
    /// as an intermittent "Connection refused" (#2733). Windows' HTTP.sys binds both families for <c>localhost</c>, and
    /// may refuse an address prefix without elevation, so it keeps <c>localhost</c>.
    /// </remarks>
    protected static string LoopbackHost => OperatingSystem.IsWindows() ? "localhost" : "127.0.0.1";

    void Establish()
    {
        _port = GetAvailablePort();
        var logger = NullLogger<HttpListenerEndpointMapper>.Instance;
        _endpointMapper = new HttpListenerEndpointMapper(logger, $"http://{LoopbackHost}:{_port}/");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        services.AddSingleton<IAuthentication, NoAuthentication>();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{LoopbackHost}:{_port}/")
        };

        _testDirectory = Path.Combine(Path.GetTempPath(), $"arc_specs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    protected void StartEndpointMapper()
    {
        _endpointMapper.Start(_serviceProvider);
    }

    protected void CreateTestFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_testDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
    }

    void Destroy()
    {
        _endpointMapper.Stop().GetAwaiter().GetResult();
        _endpointMapper.Dispose();
        _httpClient.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _endpointMapper.Stop();
        _endpointMapper.Dispose();
        _httpClient.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    class NoAuthentication : IAuthentication
    {
        public bool HasHandlers => false;

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context) =>
            Task.FromResult(AuthenticationResult.Anonymous);
    }
}
