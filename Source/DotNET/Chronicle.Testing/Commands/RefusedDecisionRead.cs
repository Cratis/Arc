// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Represents a protected decision read that Chronicle refuses to guard.
/// </summary>
/// <param name="CommandType">The <c>[ProtectedDecision]</c> command that declares the read.</param>
/// <param name="ReadModelType">The read model type of the <see cref="DecisionRead{T}"/>.</param>
/// <param name="Reason">Why Chronicle refuses the read model's shape.</param>
public record RefusedDecisionRead(Type CommandType, Type ReadModelType, DecisionReadRefusalReason Reason);
