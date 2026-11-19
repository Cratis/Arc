// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents what an artifact requires of the availability of the part it lives in. Every gate has to hold.
/// </summary>
/// <param name="Gates">The gates the artifact carries.</param>
public record AvailabilityModel(IEnumerable<AvailabilityGateModel> Gates);
