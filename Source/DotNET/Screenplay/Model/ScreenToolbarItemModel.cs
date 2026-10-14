// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a menu item opening command input or navigating to a proven screen destination.
/// </summary>
/// <param name="Name">The stable item name.</param>
/// <param name="Command">The command whose input dialog is opened, empty for a navigation item.</param>
/// <param name="Label">The literal label, or null when unread.</param>
public record ScreenToolbarItemModel(string Name, string Command, string? Label)
{
    /// <summary>
    /// Gets the route destination for a navigation item rather than a command opener.
    /// </summary>
    public ScreenNavigationModel? Navigation { get; init; }
}
