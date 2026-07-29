// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents a name a user interface file imports, together with where it imported it from.
/// </summary>
/// <param name="Name">The name as the module exports it, rather than as the importing file renames it.</param>
/// <param name="Module">The module specifier, exactly as it was written.</param>
/// <remarks>
/// The name alone answers what a screen binds while the query is one its own slice declares. It stops answering the
/// moment the query is declared elsewhere, because a real application declares <c>All</c> once per read model and the
/// name says nothing about which of them was meant. Where the module was written is what says it, so the two travel
/// together.
/// </remarks>
public record ScreenImport(string Name, string Module);
