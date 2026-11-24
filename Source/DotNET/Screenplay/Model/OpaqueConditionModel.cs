// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A condition that is a class, whose rules the model cannot state.
/// </summary>
/// <param name="Name">The name of the condition.</param>
public record OpaqueConditionModel(string Name) : AvailabilityConditionModel;
