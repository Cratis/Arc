// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A command or read model.
/// </summary>
/// <param name="Name">The full name of the command type, or of the type that declares the query.</param>
/// <param name="Kind">Either <c>command</c> or <c>readModel</c>.</param>
/// <param name="Module">The module of its slice; empty when it has none.</param>
/// <param name="Feature">The feature of its slice; empty when it has none.</param>
/// <param name="Slice">Its slice; empty when it has none.</param>
/// <param name="Gates">The gates down its path, outermost first; every one must hold. Empty when nothing gates it.</param>
public record ManifestPart(string Name, string Kind, string Module, string Feature, string Slice, IReadOnlyList<ManifestExpression> Gates);
