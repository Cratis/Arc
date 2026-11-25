// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The names typed declarations take at run time, which are the names Screenplay states.
/// </summary>
public static class ContextNames
{
    /// <summary>
    /// Gets the name a read model is provided under: the name of its type in camelCase.
    /// </summary>
    /// <param name="readModel">The type of the provided read model.</param>
    /// <returns>The name, as in <c>$context.vessel</c>.</returns>
    public static string Of(Type readModel) => CamelCase(readModel.Name);

    /// <summary>
    /// Converts a name to camelCase.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The name with its first character in lower case.</returns>
    public static string CamelCase(string name) =>
        name.Length == 0 || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
