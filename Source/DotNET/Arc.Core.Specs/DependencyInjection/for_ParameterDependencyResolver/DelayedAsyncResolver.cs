// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

/// <summary>
/// An asynchronous resolver that owns every parameter and completes when the test releases it.
/// </summary>
/// <param name="dependency">The dependency to resolve to, which may be null.</param>
public sealed class DelayedAsyncResolver(object? dependency) : IAsyncParameterDependencyResolver
{
    readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the number of times the synchronous contract was called.
    /// </summary>
    public int SynchronousCalls { get; private set; }

    /// <summary>
    /// Gets the number of asynchronous resolutions started.
    /// </summary>
    public int AsynchronousCalls { get; private set; }

    /// <summary>
    /// Completes the pending resolution.
    /// </summary>
    public void Release() => _gate.SetResult();

    /// <inheritdoc/>
    public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
    {
        SynchronousCalls++;
        dependency = null;
        return false;
    }

    /// <inheritdoc/>
    public async ValueTask<ParameterDependencyResolution> Resolve(IServiceProvider serviceProvider, ParameterInfo parameter)
    {
        AsynchronousCalls++;
        await _gate.Task;
        return ParameterDependencyResolution.Resolved(dependency);
    }
}
