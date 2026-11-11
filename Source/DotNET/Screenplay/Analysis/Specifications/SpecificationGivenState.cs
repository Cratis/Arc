// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// What one state a specification starts from is.
/// </summary>
/// <param name="Kind">What the state is.</param>
/// <param name="Source">The concrete occurrence source, when stated.</param>
/// <param name="Route">The proven occurrence route, when stated.</param>
internal sealed record SpecificationGivenState(SpecificationStateKind Kind, LiteralSource? Source, SpecificationEventRouteModel? Route);
