// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// 'or': holds when any part holds.
/// </summary>
/// <param name="Parts">The parts, in authored order.</param>
public record AvailabilityOr(IReadOnlyList<AvailabilityExpression> Parts) : AvailabilityExpression;
