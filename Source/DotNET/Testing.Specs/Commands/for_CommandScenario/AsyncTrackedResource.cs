// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.for_CommandScenario;

/// <summary>
/// A resource implementing both <see cref="IAsyncDisposable"/> and <see cref="IDisposable"/> that records
/// which disposal path was taken and how many times.
/// </summary>
public sealed class AsyncTrackedResource : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the number of times <see cref="DisposeAsync"/> has been called.
    /// </summary>
    public int AsyncDisposeCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="Dispose"/> has been called.
    /// </summary>
    public int DisposeCount { get; private set; }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        AsyncDisposeCount++;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => DisposeCount++;
}
