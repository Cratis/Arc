// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Represents the outcome of asking an <see cref="IAsyncParameterDependencyResolver"/> to resolve a parameter.
/// </summary>
/// <param name="IsResolved">Whether the resolver owns the parameter. An owned parameter may still resolve to null.</param>
/// <param name="Dependency">The resolved dependency, which may be null even when <paramref name="IsResolved"/> is true.</param>
public readonly record struct ParameterDependencyResolution(bool IsResolved, object? Dependency)
{
    /// <summary>
    /// Gets the resolution for a parameter the resolver does not own.
    /// </summary>
    public static ParameterDependencyResolution NotResolved => default;

    /// <summary>
    /// Creates the resolution for an owned parameter.
    /// </summary>
    /// <param name="dependency">The resolved dependency, which may be null.</param>
    /// <returns>The resolution.</returns>
    public static ParameterDependencyResolution Resolved(object? dependency) => new(true, dependency);
}
