// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Optional extension of <see cref="ICanResolveParameterDependency"/> for resolvers whose dependency
/// is acquired asynchronously, so command parameter binding yields instead of blocking its scheduler.
/// </summary>
/// <remarks>
/// Resolvers are still discovered through <see cref="ICanResolveParameterDependency"/>. Binding that
/// awaits (command <c>Handle()</c> and <c>Provide()</c> parameters) calls <see cref="Resolve"/> on an
/// implementing resolver; consumers that cannot await keep calling the synchronous
/// <see cref="ICanResolveParameterDependency.TryResolve"/>. Resolvers that do not implement this
/// interface continue to work unchanged.
/// </remarks>
public interface IAsyncParameterDependencyResolver : ICanResolveParameterDependency
{
    /// <summary>
    /// Resolves the supplied parameter asynchronously, including a recognized dependency that resolves to null.
    /// </summary>
    /// <param name="serviceProvider">The invocation's service provider.</param>
    /// <param name="parameter">The parameter and its declarations.</param>
    /// <returns>The resolution, distinguishing a parameter this resolver does not own from one that resolved to null.</returns>
    ValueTask<ParameterDependencyResolution> Resolve(IServiceProvider serviceProvider, ParameterInfo parameter);
}
