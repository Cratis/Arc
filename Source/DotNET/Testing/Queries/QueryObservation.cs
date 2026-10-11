// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// Holds the first query result and reads subsequent buffered emissions until disposed.
/// </summary>
public sealed class QueryObservation : IDisposable, IAsyncDisposable
{
    readonly ObservedQueryEmissions? _emissions;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryObservation"/> class.
    /// </summary>
    /// <param name="initial">The first emission or pipeline rejection.</param>
    /// <param name="emissions">The buffered subscription, if any.</param>
    internal QueryObservation(QueryResult initial, ObservedQueryEmissions? emissions = null)
    {
        Initial = initial;
        _emissions = emissions;
    }

    /// <summary>
    /// Gets the first emission or the pipeline's rejection.
    /// </summary>
    public QueryResult Initial { get; private set; }

    /// <summary>
    /// Reads the next emission, not necessarily the next distinct value.
    /// </summary>
    /// <param name="timeout">The wait deadline; defaults to five seconds.</param>
    /// <param name="cancellationToken">Cancellation for this read.</param>
    /// <returns>The next result, including stream errors or completion.</returns>
    /// <exception cref="TimeoutException">No emission arrives before the deadline.</exception>
    /// <exception cref="ObjectDisposedException">The observation has been disposed.</exception>
    public Task<QueryResult> Next(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        return _emissions?.Read(timeout ?? TimeSpan.FromSeconds(5), cancellationToken) ?? Task.FromResult(Initial);
    }

    /// <summary>
    /// Releases the subscription and its owned scope without disposing the subject.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _emissions?.Dispose();
    }

    /// <summary>
    /// Asynchronously releases enumeration and its owned scope without disposing the subject.
    /// </summary>
    /// <returns>The disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_emissions is not null)
        {
            await _emissions.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits for the first emission after the scenario has taken ownership of this observation.
    /// </summary>
    /// <param name="timeout">The first-emission deadline.</param>
    /// <param name="cancellationToken">Cancellation for the initial wait.</param>
    /// <returns>The initialization operation.</returns>
    internal async Task ReadInitial(TimeSpan timeout, CancellationToken cancellationToken) =>
        Initial = await _emissions!.Read(timeout, cancellationToken);
}
