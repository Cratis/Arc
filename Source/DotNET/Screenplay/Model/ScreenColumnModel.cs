// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a column of a table a screen shows.
/// </summary>
/// <param name="Property">The property of the row the column shows, exactly as the component names it.</param>
/// <param name="Label">The header of the column when the component writes it as text, otherwise null.</param>
public record ScreenColumnModel(string Property, string? Label);
