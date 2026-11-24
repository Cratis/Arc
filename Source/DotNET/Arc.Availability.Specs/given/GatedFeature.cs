// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.given.GatedNamespace;

/// <summary>
/// Gates everything in this namespace to the Plus audience.
/// </summary>
[AvailableFor<TypedDeclarations.Plus>]
public class GatedFeature : AvailabilityScope;
