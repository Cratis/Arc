// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>Explicit extension rules, evaluated in order. The default check is Arc-only.</summary>
public sealed class CommandWiringOptions
{
    /// <summary>Gets the rules. The first applicable rule owns a parameter's metadata check.</summary>
    public IReadOnlyList<ICommandWiringRule> Rules { get; init; } = [];
}
