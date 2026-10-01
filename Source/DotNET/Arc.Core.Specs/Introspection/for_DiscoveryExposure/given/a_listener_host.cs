// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Cratis.Arc.Authentication;
using Cratis.Arc.Commands;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.given;

public class a_listener_host : Specification
{
    protected HttpStatusCode[] _statuses;
    protected ConcurrentQueue<string> _warnings = new();

    protected async Task RequestDiscovery(string[] args)
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var address = $"http://127.0.0.1:{port}/";
        var builder = new ArcApplicationBuilder(args);
        builder.AddCratisArc(options =>
        {
            options.Hosting.ApplicationUrl = address;
            options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider);
        });
        var authentication = Substitute.For<IAuthentication>();
        authentication.HasHandlers.Returns(false);
        builder.Services.AddSingleton(authentication);
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns([]);
        builder.Services.AddSingleton(commands);
        var queries = Substitute.For<IQueryPerformerProviders>();
        queries.Performers.Returns([]);
        builder.Services.AddSingleton(queries);
        builder.Logging.AddProvider(new WarningLoggerProvider(_warnings));
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            var statuses = new List<HttpStatusCode>();
            foreach (var path in new[] { "/.cratis/commands", "/.cratis/queries", "/.cratis/users", "/.cratis/tenants", "/.cratis/identity-details/schema" })
            {
                using var response = await client.GetAsync(path);
                statuses.Add(response.StatusCode);
            }
            _statuses = [.. statuses];
        }
        finally
        {
            await app.StopAsync();
        }
    }

    sealed class WarningLoggerProvider(ConcurrentQueue<string> warnings) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new WarningLogger(warnings);
        public void Dispose() { }
    }

    sealed class WarningLogger(ConcurrentQueue<string> warnings) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                warnings.Enqueue(formatter(state, exception));
            }
        }
    }
}
