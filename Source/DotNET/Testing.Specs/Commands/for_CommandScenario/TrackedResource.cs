// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.for_CommandScenario;

/// <summary>
/// A disposable resource that records how many times it was disposed and whether it was used.
/// </summary>
public sealed class TrackedResource : IDisposable
{
    /// <summary>
    /// Gets the number of times <see cref="Dispose"/> has been called.
    /// </summary>
    public int DisposeCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Touch"/> has been called.
    /// </summary>
    public bool Touched { get; private set; }

    /// <summary>
    /// Marks the resource as used.
    /// </summary>
    public void Touch() => Touched = true;

    /// <inheritdoc/>
    public void Dispose() => DisposeCount++;
}
