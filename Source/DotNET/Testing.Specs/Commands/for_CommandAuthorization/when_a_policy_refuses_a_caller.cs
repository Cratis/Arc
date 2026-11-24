// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_policy_refuses_a_caller : given.an_evaluation
{
    void Establish() => Behavior.Evaluate = (_, _) => throw new TargetInvocationException(new TenantMembershipDenied());
    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
    [Fact] void should_deny() => Result.ShouldBeDenied();
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
