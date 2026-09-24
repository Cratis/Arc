// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// Records logging callbacks so teardown specs can await the late emission's terminal path, not a clock.
/// </summary>
/// <param name="signals">The pulse to notify after a logging callback.</param>
public sealed class observed_logger(condition_pulse signals) : ILogger<ObservableQueryDemultiplexer>
{
    int _count;

    /// <summary>
    /// Gets the number of logging callbacks observed.
    /// </summary>
    public int Count => Volatile.Read(ref _count);

    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => new Scope();

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Interlocked.Increment(ref _count);
        signals.Signal();
    }

    sealed class Scope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
