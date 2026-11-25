// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// One decision to suspend or resume a part, as a <see cref="ISuspensionSource"/> reports it.
/// </summary>
/// <param name="Path">The part path: a module, a feature, a slice or a part, as <see cref="PartPaths"/> names it.</param>
/// <param name="Level">The <see cref="AudienceLevel"/> the decision was made at.</param>
/// <param name="IsSuspended">True for a suspension, false for a resume.</param>
/// <param name="DecidedAt">When the decision was made; the newest decision wins per level and path.</param>
public record PartSuspensionDecision(string Path, AudienceLevel Level, bool IsSuspended, DateTimeOffset DecidedAt);
