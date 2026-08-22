// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the cancellation lifetime of a trackable streaming observable query subscription.
/// </summary>
/// <param name="cancellationTokenSource">The linked <see cref="CancellationTokenSource"/> that drives the subscription's callbacks or background streaming loop.</param>
/// <remarks>
/// Subject callbacks and async-enumerable background loops are bound to this source's token. Disposing the tracked
/// subscription cancels it before its remaining resources are torn down, so an unsubscribe or connection teardown
/// stops in-flight work before disposing the resources that work uses.
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
