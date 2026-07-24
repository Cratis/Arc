// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a trackable subscription over an <see cref="IAsyncEnumerable{T}"/>-backed observable query.
/// </summary>
/// <param name="cancellationTokenSource">The linked <see cref="CancellationTokenSource"/> that drives the background streaming loop.</param>
/// <remarks>
/// The background loop that pushes an async-enumerable's items to the client is bound to this source's token.
/// Disposing the subscription cancels it, so an unsubscribe — or a connection teardown that disposes the tracked
/// subscriptions — actually stops the stream instead of leaving it running for the life of the connection.
/// </remarks>
internal sealed class StreamingQuerySubscription(CancellationTokenSource cancellationTokenSource) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed — the stream is already stopping; nothing more to do.
        }

        cancellationTokenSource.Dispose();
    }
}
