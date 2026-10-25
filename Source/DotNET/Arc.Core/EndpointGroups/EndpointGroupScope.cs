// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// Represents the endpoint groups served on the local port a request arrived on.
/// </summary>
public sealed class EndpointGroupScope
{
    /// <summary>
    /// The scope of a port that is not assigned to any group: it serves the default group only.
    /// </summary>
    public static readonly EndpointGroupScope Default = new(true, []);

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointGroupScope"/> class.
    /// </summary>
    /// <param name="includesDefault">Whether the default group is served.</param>
    /// <param name="groups">The named groups that are served.</param>
    public EndpointGroupScope(bool includesDefault, IEnumerable<string> groups)
    {
        IncludesDefault = includesDefault;
        Groups = new HashSet<string>(groups, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets a value indicating whether the default group is served.
    /// </summary>
    public bool IncludesDefault { get; }

    /// <summary>
    /// Gets the named groups that are served.
    /// </summary>
    public IReadOnlySet<string> Groups { get; }

    /// <summary>
    /// Checks whether a group is served.
    /// </summary>
    /// <param name="group">The name of the group, or <see langword="null"/> for the default group.</param>
    /// <returns>True if the group is served, false if not.</returns>
    public bool Serves(string? group) => group is null ? IncludesDefault : Groups.Contains(group);

    /// <summary>
    /// Checks whether this scope serves exactly the same groups as another.
    /// </summary>
    /// <param name="other">The other <see cref="EndpointGroupScope"/>.</param>
    /// <returns>True if both serve the same groups, false if not.</returns>
    public bool IsSameAs(EndpointGroupScope other) =>
        IncludesDefault == other.IncludesDefault && Groups.SetEquals(other.Groups);
}
