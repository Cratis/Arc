// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// 'with': holds when the named availability block holds.
/// </summary>
/// <param name="Name">The name of the block.</param>
public record AvailableWith(string Name) : AvailabilityExpression;
