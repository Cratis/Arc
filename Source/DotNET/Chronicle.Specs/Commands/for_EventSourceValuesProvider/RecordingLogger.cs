// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider;

/// <summary>
/// Records what was logged. A substitute would report nothing here — the source-generated log methods check
/// <see cref="ILogger.IsEnabled"/> first, and a substitute answers false by default, so the call never happens.
/// </summary>
/// <typeparam name="T">Type the logger is for.</typeparam>
public class RecordingLogger<T> : ILogger<T>
{
    /// <summary>
    /// Gets the entries that were logged, with the exception each carried.
    /// </summary>
    public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, exception));
}
