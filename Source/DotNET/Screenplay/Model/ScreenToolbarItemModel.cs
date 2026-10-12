// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a menu item opening a command dialog of its slice.
/// </summary>
/// <param name="Name">The stable item name.</param>
/// <param name="Command">The command whose input dialog is opened.</param>
/// <param name="Label">The literal label, or null when unread.</param>
public record ScreenToolbarItemModel(string Name, string Command, string? Label);
