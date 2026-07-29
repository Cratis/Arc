// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a provider that can resolve read models by key for a command, so a read model backed by any store — Chronicle,
/// Entity Framework Core, or another provider — can be injected into command-scoped code.
/// </summary>
/// <remarks>
/// A read model is injectable into command-scoped code (a <c>CommandValidator&lt;&gt;</c>, <c>Provide()</c>, or
/// <c>Handle()</c>) because it is resolvable by the command's resolved key. The command pipeline resolves each read model
/// dependency from DI by type, and a provider contributes a command-scoped, by-key resolver for the read model types it
/// owns through this abstraction. Providers coexist without claiming each other's read model types — each reports only
/// the types it can resolve through <see cref="ReadModelTypes"/>.
/// </remarks>
public interface ICanResolveReadModelForCommand
{
    /// <summary>
    /// Gets the read model types this provider can resolve by key for a command.
    /// </summary>
    IEnumerable<Type> ReadModelTypes { get; }

    /// <summary>
    /// Resolves the read model of the given type for the current command context, keyed by the command's resolved key.
    /// </summary>
    /// <param name="readModelType">The type of read model to resolve.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> to resolve from.</param>
    /// <returns>The resolved read model instance, or null when no instance exists for the command's key.</returns>
    Task<object?> Resolve(Type readModelType, CommandContext commandContext);
}
