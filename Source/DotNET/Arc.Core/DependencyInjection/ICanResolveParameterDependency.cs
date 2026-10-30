// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Resolves a parameter dependency using its metadata before falling back to type-based resolution.
/// </summary>
public interface ICanResolveParameterDependency
{
    /// <summary>
    /// Tries to resolve the supplied parameter, including a recognized dependency that resolves to null.
    /// </summary>
    /// <param name="serviceProvider">The invocation's service provider.</param>
    /// <param name="parameter">The parameter and its declarations.</param>
    /// <param name="dependency">The resolved dependency, which may be null.</param>
    /// <returns>Whether this resolver owns the parameter.</returns>
    bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency);

    /// <summary>
    /// Tries to resolve the supplied parameter asynchronously, including an owned null dependency.
    /// </summary>
    /// <param name="serviceProvider">The invocation's service provider.</param>
    /// <param name="parameter">The parameter and its declarations.</param>
    /// <returns>Whether this resolver owns the parameter and its resolved dependency.</returns>
    /// <remarks>
    /// The default implementation adapts synchronous resolvers without changing their behavior. Override this
    /// method to await I/O for Handle or Provide parameters. The original parameter retains its attributes and
    /// nullable declaration. A resolved null is owned and does not fall back to DI; an unresolved result lets the
    /// next resolver, and finally DI, try. Constructor injection remains synchronous.
    /// </remarks>
    ValueTask<(bool Resolved, object? Dependency)> TryResolveAsync(IServiceProvider serviceProvider, ParameterInfo parameter)
    {
        var resolved = TryResolve(serviceProvider, parameter, out var dependency);
        return new((resolved, dependency));
    }
}
