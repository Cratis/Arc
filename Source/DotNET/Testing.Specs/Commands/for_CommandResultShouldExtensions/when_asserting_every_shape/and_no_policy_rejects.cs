// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_every_shape;

/// <summary>
/// The compatibility pin, and the coverage pin at once. Every assertion still passes exactly what it always did for
/// a result that satisfies it - a consumer who installs no policy must see no difference at all - and every one of
/// them reaches the seam, so a policy is not silently absent from eight of the nine.
/// </summary>
/// <remarks>
/// These assertions had no direct coverage of any kind before this spec project existed.
/// </remarks>
[Collection(AssertionPolicyCollection.Name)]
public class and_no_policy_rejects : Specification
{
    CommandResult _successful;
    CommandResult _rejected;
    CommandResult _unauthorized;
    CommandResult _faulted;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _successful = new CommandResult();
        _rejected = new CommandResult { ValidationResults = [ValidationResult.Error("nope")] };
        _unauthorized = new CommandResult { IsAuthorized = false, AuthorizationFailureReason = "no" };
        _faulted = new CommandResult { ExceptionMessages = ["boom"] };
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _successful.ShouldBeSuccessful();
        _successful.ShouldBeValid();
        _successful.ShouldBeAuthorized();
        _successful.ShouldNotHaveExceptions();
        _rejected.ShouldNotBeSuccessful();
        _rejected.ShouldHaveValidationErrors();
        _rejected.ShouldHaveValidationErrorFor("nope");
        _unauthorized.ShouldNotBeAuthorized();
        _faulted.ShouldHaveExceptions();
    });

    [Fact] void should_pass_every_assertion() => _error.ShouldBeNull();

    [Fact] void should_have_consulted_the_policy_from_all_nine() => given.a_recording_policy.Consulted.ShouldContainOnly(
        nameof(CommandResultShouldExtensions.ShouldBeSuccessful),
        nameof(CommandResultShouldExtensions.ShouldBeValid),
        nameof(CommandResultShouldExtensions.ShouldBeAuthorized),
        nameof(CommandResultShouldExtensions.ShouldNotHaveExceptions),
        nameof(CommandResultShouldExtensions.ShouldNotBeSuccessful),
        nameof(CommandResultShouldExtensions.ShouldHaveValidationErrors),
        nameof(CommandResultShouldExtensions.ShouldHaveValidationErrorFor),
        nameof(CommandResultShouldExtensions.ShouldNotBeAuthorized),
        nameof(CommandResultShouldExtensions.ShouldHaveExceptions));
}
