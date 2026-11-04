// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Disposes subscription resources using asynchronous disposal when available.
/// </summary>
internal static class SubscriptionResourceDisposal
{
    /// <summary>
    /// Disposes an owned resource, preferring its asynchronous disposal implementation.
    /// </summary>
    /// <param name="resource">The resource to dispose.</param>
    /// <returns>The asynchronous disposal operation, or a completed task for synchronous disposal.</returns>
    internal static ValueTask DisposeAsync(IDisposable resource)
    {
        if (resource is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        resource.Dispose();
        return ValueTask.CompletedTask;
    }
}
