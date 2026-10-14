// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents a name a user interface file imports, together with where it imported it from.
/// </summary>
/// <param name="Name">The name as the module exports it, rather than as the importing file renames it.</param>
/// <param name="Module">The module specifier, exactly as it was written.</param>
/// <remarks>
/// A real application declares names such as <c>All</c> once per read model. The module path identifies the declaring
/// slice, and the exported name identifies its command or query, so both travel together.
/// </remarks>
public record ScreenImport(string Name, string Module);
