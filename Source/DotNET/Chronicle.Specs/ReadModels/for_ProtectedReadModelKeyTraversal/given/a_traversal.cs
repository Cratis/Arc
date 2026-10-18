// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal.given;

public class a_traversal : Specification
{
    protected RecordingLogger _logger;
    private protected ProtectedReadModelKeyTraversal _traversal;
    protected ProtectedReadModelKeyOptions _options;

    void Establish()
    {
        _logger = new();
        _options = new();
        _traversal = new(Options.Create(_options), _logger);
    }

    protected sealed class RecordingLogger : ILogger<ProtectedReadModelKeyTraversal>
    {
        public List<(LogLevel Level, string Text)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
