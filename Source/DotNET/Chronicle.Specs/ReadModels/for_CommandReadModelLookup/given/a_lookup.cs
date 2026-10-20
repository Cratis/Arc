// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup.given;

public class a_lookup : Specification
{
    protected CommandContext _context;
    protected RecordingLogger _logger;
    ServiceProvider _services;

    protected void Configure(object command, bool requireGeneratedMetadata = false)
    {
        _logger = new();
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(_logger);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReadProtection:RequireGeneratedMetadata"] = requireGeneratedMetadata.ToString()
        }).Build();
        _services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddSingleton(factory).BuildServiceProvider();
        var values = new CommandContextValues { [WellKnownCommandContextKeys.EventSourceId] = (EventSourceId)"subject" };
        _context = new(CorrelationId.New(), command.GetType(), command, [], values, ServiceProvider: _services);
    }

    void Destroy() => _services.Dispose();

    /// <summary>
    /// Logger that formats each entry as it is written.
    /// </summary>
    protected sealed class RecordingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
