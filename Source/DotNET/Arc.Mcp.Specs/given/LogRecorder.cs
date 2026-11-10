// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp.given;

/// <summary>
/// Records messages and structured state without filtering their level.
/// </summary>
internal sealed class LogRecorder<T> : ILogger<T>
{
    internal List<(LogLevel Level, EventId Event, string Message, object?[] Values)> Entries { get; } = [];

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = (state as IEnumerable<KeyValuePair<string, object?>>)?.Select(_ => _.Value).ToArray() ?? [];
        Entries.Add((logLevel, eventId, formatter(state, exception), values));
    }
}
