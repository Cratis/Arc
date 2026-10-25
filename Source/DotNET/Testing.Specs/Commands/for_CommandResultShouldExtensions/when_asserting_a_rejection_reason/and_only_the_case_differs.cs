// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_rejection_reason;

[Collection(AssertionPolicyCollection.Name)]
public class and_only_the_case_differs : Specification
{
    Exception _error;

    void Establish() => given.a_recording_policy.Reset();
    void Because() => _error = Catch.Exception(() => new CommandResult { ValidationResults = [ValidationResult.Error("Rejected")] }.ShouldBeRejected("rejected"));

    [Fact] void should_compare_ordinally() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
}
