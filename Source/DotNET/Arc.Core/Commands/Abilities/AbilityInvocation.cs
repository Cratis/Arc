// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Abilities;

/// <summary>
/// One request to run an ability of a system, handed to the <see cref="IAbilityInvoker"/>.
/// </summary>
/// <param name="System">The system that owns the ability, as declared in the model.</param>
/// <param name="Ability">The ability name.</param>
/// <param name="Inputs">The typed inputs by name.</param>
/// <param name="IdempotencyKey">
/// The key the extension receives. It is stable for the same call in the same command execution, never includes an
/// attempt, and differs between Execute and Compensate.
/// </param>
/// <param name="Causation">The command, correlation, operation and phase that caused the invocation.</param>
public record AbilityInvocation(
    string System,
    string Ability,
    IReadOnlyDictionary<string, object?> Inputs,
    string IdempotencyKey,
    IReadOnlyDictionary<string, string> Causation);
