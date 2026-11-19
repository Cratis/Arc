// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.Abilities;

namespace Cratis.Arc.Commands.for_SystemAbilities.given;

public class RecordingAbilityInvoker : IAbilityInvoker
{
    public List<AbilityInvocation> Invocations { get; } = [];

    public string FailAbility { get; set; }

    public Task<AbilityInvocationResult> Invoke(AbilityInvocation invocation, CancellationToken cancellationToken)
    {
        Invocations.Add(invocation);
        return Task.FromResult(invocation.Ability == FailAbility
            ? AbilityInvocationResult.FailedWith("ledger unavailable")
            : AbilityInvocationResult.ReturnedWith());
    }
}
