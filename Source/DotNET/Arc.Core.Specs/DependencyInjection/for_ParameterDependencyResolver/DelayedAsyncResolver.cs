// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

/// <summary>
/// Owns an explicitly registered substitute, rather than a discoverable production extension point.
/// </summary>
internal sealed class DelayedAsyncResolver
{
    readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayedAsyncResolver"/> class.
    /// </summary>
    /// <param name="dependency">The dependency to resolve to, which may be null.</param>
    public DelayedAsyncResolver(object? dependency)
    {
        Resolver = Substitute.For<IAsyncParameterDependencyResolver>();
        Resolver.TryResolve(Arg.Any<IServiceProvider>(), Arg.Any<ParameterInfo>(), out Arg.Any<object?>()).Returns(call =>
        {
            SynchronousCalls++;
            call[2] = null;
            return false;
        });
        Resolver.Resolve(Arg.Any<IServiceProvider>(), Arg.Any<ParameterInfo>()).Returns(_ => ResolvePending(dependency));
    }

    /// <summary>
    /// Gets the resolver registered explicitly by the spec.
    /// </summary>
    public IAsyncParameterDependencyResolver Resolver { get; }

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

    async ValueTask<ParameterDependencyResolution> ResolvePending(object? dependency)
    {
        AsynchronousCalls++;
        await _gate.Task;
        return ParameterDependencyResolution.Resolved(dependency);
    }
}
