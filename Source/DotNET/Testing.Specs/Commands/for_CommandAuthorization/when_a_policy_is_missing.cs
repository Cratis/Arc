// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_policy_is_missing : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.UnknownPolicyCommand());
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_explain_the_configuration() => Result.ReasonCode.ShouldEqual("invalid-configuration");
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
