// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Retains a literal route claim, including unresolved endpoints that must not win against a readable sibling.
/// </summary>
/// <param name="Path">The absolute literal route pattern.</param>
/// <param name="Screen">The unique emitted screen, when resolved.</param>
internal sealed record RouteEntry(string Path, RouteScreen? Screen);
