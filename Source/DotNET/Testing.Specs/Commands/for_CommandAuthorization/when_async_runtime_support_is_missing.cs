// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_async_runtime_support_is_missing : given.an_evaluation
{
    void Establish() => Behavior.Evaluate = (_, _) => throw new AsynchronousAuthorizationRequired();
    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_the_runtime() => Result.ReasonCode.ShouldEqual("async-runtime-required");
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
