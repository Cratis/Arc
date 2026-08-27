// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Represents one complete scenario contribution awaiting exact shared placement.
/// </summary>
/// <param name="PlacementRequests">The exact artifact placement requests.</param>
/// <param name="Facts">The scenario facts staged atomically.</param>
internal sealed record ArcSpecificationFactCandidate(
    IReadOnlyList<DotNetSourcePlacementRequest> PlacementRequests,
    IReadOnlyList<GenerationFact> Facts);
