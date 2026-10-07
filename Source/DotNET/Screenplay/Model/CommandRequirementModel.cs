// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>Represents an acceptance condition recovered from a provisioning guard.</summary>
/// <param name="Condition">The condition that must hold.</param>
/// <param name="Message">The literal rejection message.</param>
public record CommandRequirementModel(ConditionModel Condition, string Message);
