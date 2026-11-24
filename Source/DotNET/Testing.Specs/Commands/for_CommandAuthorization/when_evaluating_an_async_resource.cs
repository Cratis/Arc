// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_evaluating_an_async_resource : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
    [Fact] void should_authorize() => Result.ShouldBeAuthorized();
    [Fact] void should_evaluate_the_policy_once() => Behavior.PolicyCalls.ShouldEqual(1);
    [Fact] void should_not_handle() => Behavior.HandlerCalls.ShouldEqual(0);
    [Fact] void should_not_validate() => Behavior.ValidatorCalls.ShouldEqual(0);
    [Fact] void should_not_provide() => Behavior.ProvideCalls.ShouldEqual(0);
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
