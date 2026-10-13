// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the command input openers recovered from a data page's menu.
/// </summary>
/// <param name="Name">The stable toolbar name.</param>
/// <param name="Items">The items in source order.</param>
public record ScreenToolbarModel(string Name, IEnumerable<ScreenToolbarItemModel> Items);
