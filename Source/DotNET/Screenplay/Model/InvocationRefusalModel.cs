// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents one ordered invocation refusal branch; no productions means acknowledge.
/// </summary>
/// <param name="Selector">The category: any (excluding authorization), validation, authorization or constraint.</param>
/// <param name="Constraint">The exact constraint name, or null for an unrestricted category.</param>
/// <param name="Produces">The ordered events appended when this branch matches.</param>
public record InvocationRefusalModel(string Selector, string? Constraint, IReadOnlyList<ProducesModel> Produces);
