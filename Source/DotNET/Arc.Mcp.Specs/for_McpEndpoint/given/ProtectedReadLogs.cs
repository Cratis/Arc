// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Captures formatted messages, exception text, and structured values from the real host, including MCP audit logs.
/// </summary>
public sealed class ProtectedReadLogs : ILoggerProvider
{
    /// <summary>Gets the captured category and rendered log content.</summary>
    public ConcurrentQueue<(string Category, string Content)> Entries { get; } = [];

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new Recorder(categoryName, Entries);

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    sealed class Recorder(string category, ConcurrentQueue<(string Category, string Content)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue((category, formatter(state, exception)));
            if (exception is not null)
            {
                entries.Enqueue((category, exception.ToString()));
            }
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                foreach (var value in values)
                {
                    entries.Enqueue((category, value.Value?.ToString() ?? string.Empty));
                }
            }
        }
    }
}
