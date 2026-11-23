// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_resource_policy_denies : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(false));
    [Fact] void should_deny() => Result.ShouldBeDenied();
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
