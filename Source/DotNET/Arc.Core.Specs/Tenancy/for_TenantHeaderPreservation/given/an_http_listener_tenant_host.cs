// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantHeaderPreservation.given;

public class an_http_listener_tenant_host : Specification
{
    protected ArcOptions _options;
    protected IHttpRequestContextAccessor _accessor;
    protected ILogger<HttpListenerEndpointMapper> _logger;
    protected Exception _error;
    protected List<string> _warnings;
    HttpListenerEndpointMapper _mapper;
    ServiceProvider _services;

    protected int Warnings => _warnings.Count;

    void Establish()
    {
        _options = new ArcOptions();
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var host = OperatingSystem.IsWindows() ? "localhost" : "127.0.0.1";
        _options.Hosting.ApplicationUrl = $"http://{host}:{port}/";
        _accessor = Substitute.For<IHttpRequestContextAccessor>();
        _warnings = [];
        _logger = new RecordingLogger(_warnings);
    }

    protected void StartWith(ITenantIdResolver resolver)
    {
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Options.Create(_options))
            .AddSingleton(resolver)
            .BuildServiceProvider();
        _mapper = new HttpListenerEndpointMapper(_logger, _options.Hosting.ApplicationUrl);
        _error = Catch.Exception(() => _mapper.Start(_services));
    }

    async Task Destroy()
    {
        if (_mapper is not null)
        {
            await _mapper.Stop();
            _mapper.Dispose();
        }
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }

    sealed class RecordingLogger(List<string> warnings) : ILogger<HttpListenerEndpointMapper>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                warnings.Add(formatter(state, exception));
            }
        }
    }
}
