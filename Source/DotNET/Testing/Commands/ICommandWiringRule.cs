// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>Inspects one extension's parameter binding metadata without executing its resolver.</summary>
public interface ICommandWiringRule
{
    /// <summary>Gets the exact parameter resolver types whose behavior this rule covers.</summary>
    IEnumerable<Type> CoveredResolverTypes { get; }

    /// <summary>Inspects a dependency declaration without reading command state or persistence.</summary>
    /// <param name="parameter">The command dependency metadata.</param>
    /// <returns>Not applicable, structurally supported, or an explicit failure.</returns>
    CommandWiringVerdict Inspect(CommandWiringParameter parameter);
}
