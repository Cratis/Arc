// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the field an appended event property is filled from.
/// </summary>
/// <param name="Property">The name of the appended event property.</param>
/// <param name="Field">The name of the captured field.</param>
public record CaptureMappingModel(string Property, string Field);
