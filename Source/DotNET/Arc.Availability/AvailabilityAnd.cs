// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// 'and': holds when every part holds.
/// </summary>
/// <param name="Parts">The parts, in authored order.</param>
public record AvailabilityAnd(IReadOnlyList<AvailabilityExpression> Parts) : AvailabilityExpression;
