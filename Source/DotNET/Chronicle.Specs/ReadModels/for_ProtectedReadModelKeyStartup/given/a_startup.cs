// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyStartup.given;

public class a_startup : Specification
{
    private protected ProtectedReadModelKeyStartup _startup;
    private protected RecordingLogger _logger;

    protected void Configure(Type command, bool requireMetadata, ReadProtectionMode maximumMode = ReadProtectionMode.On)
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(command);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler, handler]);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReadProtection:RequireGeneratedMetadata"] = requireMetadata.ToString(),
            ["ReadProtection:MaximumMode"] = maximumMode.ToString()
        }).Build();
        _logger = new();
        _startup = new(handlers, configuration, ProtectedKeyTraversalForSpecs.Create(), _logger);
    }

    /// <summary>
    /// Recording logger; ILogger of the internal startup type cannot be proxied by NSubstitute.
    /// </summary>
    private protected sealed class RecordingLogger : ILogger<ProtectedReadModelKeyStartup>
    {
        public int Entries { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries++;
    }
}
