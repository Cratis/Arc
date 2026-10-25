// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_new_shapes;

[Collection(AssertionPolicyCollection.Name)]
public class and_no_policy_rejects : Specification
{
    CommandResult _rejected;
    CommandResult _denied;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _rejected = new() { ValidationResults = [ValidationResult.Error("Rejected.")] };
        _denied = new() { IsAuthorized = false };
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _rejected.ShouldBeRejected();
        _rejected.ShouldBeRejected("Rejected.");
        _denied.ShouldBeDenied();
    });

    [Fact] void should_pass_all_new_shapes() => _error.ShouldBeNull();
    [Fact] void should_consult_the_policy_once_per_shape() => given.a_recording_policy.Consulted.ShouldEqual(
        [nameof(CommandResultShouldExtensions.ShouldBeRejected), nameof(CommandResultShouldExtensions.ShouldBeRejected), nameof(CommandResultShouldExtensions.ShouldBeDenied)]);
    [Fact] void should_supply_the_asserted_results() => given.a_recording_policy.Received.ShouldEqual([_rejected, _rejected, _denied]);
}
