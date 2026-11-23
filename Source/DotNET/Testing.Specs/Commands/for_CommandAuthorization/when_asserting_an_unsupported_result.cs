// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_asserting_an_unsupported_result : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For<given.ResourceCommand>(Provider, Principal);
    [Fact] void should_reject_authorized() => Assert.Throws<CommandAuthorizationAssertionFailed>(Result.ShouldBeAuthorized);
    [Fact] void should_reject_denied() => Assert.Throws<CommandAuthorizationAssertionFailed>(Result.ShouldBeDenied);
    [Fact] void should_accept_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_include_the_reason() => Assert.Throws<CommandAuthorizationAssertionFailed>(Result.ShouldBeAuthorized).Message.ShouldContain("resource-required");
    [Fact] void should_include_the_command() => Assert.Throws<CommandAuthorizationAssertionFailed>(Result.ShouldBeAuthorized).Message.ShouldContain(typeof(given.ResourceCommand).FullName);
    [Fact] void should_not_include_claim_contents() => Assert.Throws<CommandAuthorizationAssertionFailed>(Result.ShouldBeAuthorized).Message.ShouldNotContain("Writer");
}
