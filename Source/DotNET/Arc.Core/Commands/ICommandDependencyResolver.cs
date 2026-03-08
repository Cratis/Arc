// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a resolver that can provide command handler dependencies that are not registered in the service provider.
/// </summary>
/// <remarks>
/// Implementations allow the command pipeline to resolve specialized dependencies (such as read models)
/// without requiring per-type registration in the service collection at startup.
/// </remarks>
public interface ICommandDependencyResolver
{
    /// <summary>
    /// Checks whether this resolver can provide an instance of the given type.
    /// </summary>
    /// <param name="type">The dependency type to check.</param>
    /// <returns>True if the resolver can provide the dependency, false otherwise.</returns>
    bool CanResolve(Type type);

    /// <summary>
    /// Resolves an instance of the given dependency type.
    /// </summary>
    /// <param name="type">The dependency type to resolve.</param>
    /// <param name="command">The command instance being executed.</param>
    /// <param name="values">The command context values built from the command.</param>
    /// <param name="serviceProvider">The scoped <see cref="IServiceProvider"/> for the current request.</param>
    /// <returns>A <see cref="Catch{TResult}"/> containing the resolved dependency, or an exception if resolution failed.</returns>
    Catch<object> Resolve(Type type, object command, CommandContextValues values, IServiceProvider serviceProvider);
}
