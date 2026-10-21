// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Records disposal of an adapter-owned root or scope sentinel.
/// </summary>
internal sealed class ProtectedDecisionProviderLifetime : IDisposable
{
    int _disposed;

    /// <summary>
    /// Gets whether disposal has been observed.
    /// </summary>
    internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <inheritdoc/>
    public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
}
