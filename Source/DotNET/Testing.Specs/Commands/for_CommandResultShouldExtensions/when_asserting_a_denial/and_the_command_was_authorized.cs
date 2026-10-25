// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_denial;

[Collection(AssertionPolicyCollection.Name)]
public class and_the_command_was_authorized : Specification
{
    Exception _error;

    void Establish() => given.a_recording_policy.Reset();
    void Because() => _error = Catch.Exception(new CommandResult().ShouldBeDenied);

    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_explain_the_failure() => _error.Message.ShouldEqual("Expected command to be denied, but it was authorized.");
    [Fact] void should_not_consult_the_policy() => given.a_recording_policy.Consulted.ShouldBeEmpty();
}
