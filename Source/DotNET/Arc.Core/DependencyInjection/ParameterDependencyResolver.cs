// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Resolves service provider dependencies for reflected parameters while honoring nullable annotations.
/// </summary>
static class ParameterDependencyResolver
{
    /// <summary>
    /// Resolves a dependency for a parameter.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve from.</param>
    /// <param name="parameter">The parameter to resolve.</param>
    /// <param name="createException">Callback for creating an exception when a non-nullable parameter cannot be resolved or resolves to null.</param>
    /// <returns>The resolved dependency, or null when the parameter is nullable.</returns>
    /// <exception cref="CannotResolveDependency">Thrown when the dependency is registered but one of its own dependencies could not be constructed.</exception>
    public static object? Resolve(IServiceProvider serviceProvider, ParameterInfo parameter, Func<ParameterInfo, Exception> createException)
    {
        object? dependency;
        try
        {
            dependency = serviceProvider.GetService(parameter.ParameterType);
        }
        catch (InvalidOperationException failure)
        {
            // The service itself is registered, but activating it failed because one of its own
            // dependencies is missing. GetService surfaces this as a raw container exception rather
            // than a null, so translate it into an actionable error that names the member and parameter.
            throw new CannotResolveDependency(parameter, failure);
        }

        if (dependency is not null)
        {
            return dependency;
        }

        if (IsNullable(parameter))
        {
            return null;
        }

        throw createException(parameter);
    }

    /// <summary>
    /// Resolves dependencies for a set of parameters.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve from.</param>
    /// <param name="parameters">The parameters to resolve.</param>
    /// <param name="createException">Callback for creating an exception when a non-nullable parameter cannot be resolved or resolves to null.</param>
    /// <returns>The resolved dependencies in parameter order.</returns>
    public static object?[] Resolve(IServiceProvider serviceProvider, ParameterInfo[] parameters, Func<ParameterInfo, Exception> createException) =>
        parameters.Select(parameter => Resolve(serviceProvider, parameter, createException)).ToArray();

    static bool IsNullable(ParameterInfo parameter) =>
        new NullabilityInfoContext().Create(parameter).WriteState == NullabilityState.Nullable;
}
