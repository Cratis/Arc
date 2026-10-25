// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_denial;

[Collection(AssertionPolicyCollection.Name)]
public class and_the_command_was_denied : Specification
{
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _result = new() { IsAuthorized = false };
    }

    void Because() => _error = Catch.Exception(_result.ShouldBeDenied);

    [Fact] void should_pass() => _error.ShouldBeNull();
    [Fact] void should_consult_the_policy() => given.a_recording_policy.Consulted.ShouldContainOnly(nameof(CommandResultShouldExtensions.ShouldBeDenied));
    [Fact] void should_pass_the_result_to_the_policy() => given.a_recording_policy.Received.ShouldContainOnly(_result);
}
