// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>Adapts a non-generic logger substitute without proxying an internal category type.</summary>
/// <typeparam name="T">The logger category.</typeparam>
/// <param name="logger">The non-generic logger substitute.</param>
public sealed class LoggerForSpecs<T>(ILogger logger) : ILogger<T>
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => logger.BeginScope(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => logger.Log(logLevel, eventId, state, exception, formatter);
}
