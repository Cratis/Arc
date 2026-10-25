// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_new_shapes;

[Collection(AssertionPolicyCollection.Name)]
public class and_a_policy_rejects : Specification
{
    Exception[] _errors;

    void Establish()
    {
        given.a_recording_policy.Reset();
        given.a_recording_policy.Rejects = true;
    }

    void Because()
    {
        var rejected = new CommandResult { ValidationResults = [ValidationResult.Error("Rejected.")] };
        var denied = new CommandResult { IsAuthorized = false };
        _errors =
        [
            Catch.Exception(rejected.ShouldBeRejected),
            Catch.Exception(() => rejected.ShouldBeRejected("Rejected.")),
            Catch.Exception(denied.ShouldBeDenied)
        ];
    }

    [Fact] void should_propagate_every_policy_rejection() => _errors.All(_ => _ is CommandResultAssertionException).ShouldBeTrue();
    [Fact] void should_preserve_policy_diagnostics() => _errors.All(_ => _.Message == given.a_recording_policy.RejectionMessage).ShouldBeTrue();

    void Destroy() => given.a_recording_policy.Reset();
}
