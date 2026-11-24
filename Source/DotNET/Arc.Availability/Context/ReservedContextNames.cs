// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// The built-in '$context' names. No level can provide them, so none can change who a request is.
/// </summary>
public static class ReservedContextNames
{
    /// <summary>
    /// The name of the audiences that apply to the request.
    /// </summary>
    public const string Audiences = "audiences";

    /// <summary>
    /// Gets every reserved name.
    /// </summary>
    public static IReadOnlyCollection<string> All { get; } =
        ["tenant", "identity", "causedBy", "causation", "occurred", Audiences, "command", "arguments"];

    /// <summary>
    /// Determines whether a name is reserved.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns><see langword="true"/> if reserved; otherwise <see langword="false"/>.</returns>
    public static bool IsReserved(string name) => All.Contains(name, StringComparer.Ordinal);
}
