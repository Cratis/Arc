// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    void Establish()
    {
        _port = Random.Shared.Next(50000, 60000);
        var logger = NullLogger<HttpListenerEndpointMapper>.Instance;
        _endpointMapper = new HttpListenerEndpointMapper(logger, $"http://localhost:{_port}/");

        var services = new ServiceCollection();
        services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        services.AddSingleton<IAuthentication, NoAuthentication>();
        _serviceProvider = services.BuildServiceProvider();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{_port}/")
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
        _endpointMapper.StopAsync().GetAwaiter().GetResult();
        _endpointMapper.Dispose();
        _httpClient.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _endpointMapper.StopAsync();
        _endpointMapper.Dispose();
        _httpClient.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    class NoAuthentication : IAuthentication
    {
        public bool HasHandlers => false;

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context) =>
            Task.FromResult(AuthenticationResult.Anonymous);
    }
}
