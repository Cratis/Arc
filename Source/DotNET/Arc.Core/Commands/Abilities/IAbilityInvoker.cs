// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Abilities;

/// <summary>
/// The seam between a command operation and the extension host. An implementation hands the invocation to the host
/// that runs the system's extension; Arc itself ships none, so a host registers one and a test substitutes one.
/// </summary>
public interface IAbilityInvoker
{
    /// <summary>
    /// Runs one ability. Throwing, or returning <see cref="AbilityOutcomes.Failed"/>, fails the command.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<AbilityInvocationResult> Invoke(AbilityInvocation invocation, CancellationToken cancellationToken);
}
