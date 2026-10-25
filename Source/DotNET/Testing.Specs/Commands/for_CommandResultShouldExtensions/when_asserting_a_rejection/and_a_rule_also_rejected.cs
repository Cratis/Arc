// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_rejection;

[Collection(AssertionPolicyCollection.Name)]
public class and_a_rule_also_rejected : Specification
{
    Exception _error;

    void Establish() => given.a_recording_policy.Reset();

    void Because() => _error = Catch.Exception(() => new CommandResult
    {
        ValidationResults =
        [
            ValidationResult.Error("Missing account.", reason: ValidationResultReason.DependencyUnavailable),
            ValidationResult.Error("Invalid amount.")
        ]
    }.ShouldBeRejected());

    [Fact] void should_accept_the_authored_rejection() => _error.ShouldBeNull();
}
