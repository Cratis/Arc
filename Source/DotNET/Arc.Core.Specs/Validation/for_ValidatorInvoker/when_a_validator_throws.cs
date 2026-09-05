// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

/// <summary>
/// Failing closed is correct and is not what this pins: a validator that throws on hostile or partial input should
/// surface as a validation failure rather than a server error, and the cause belongs in the server log rather than
/// in the response. What it pins is that the result says so. The substitution is shaped exactly like a genuine
/// rejection - Error severity, free text, no members, no state - so without a reason the only way to recognize it
/// is to match the English sentence, which is a literal out of another project's source.
/// </summary>
public class when_a_validator_throws : given.a_validator_invoker
{
    IEnumerable<ValidationResult> _results;

    async Task Because() => _results = await _invoker.Invoke(
        new Subject("anything", "anything"),
        new ThrowingValidator(),
        string.Empty);

    [Fact] void should_fail_closed_with_a_single_result() => _results.Count().ShouldEqual(1);
    [Fact] void should_keep_saying_nothing_about_what_went_wrong() => _results.Single().Message.ShouldEqual(ValidatorInvoker.CouldNotValidateMessage);
    [Fact] void should_say_a_validator_failed() => _results.Single().Reason.ShouldEqual(ValidationResultReason.ValidatorFailed);

    /// <summary>
    /// The cost the discriminator is paid for: the throw does not add a result alongside the authored one, it
    /// replaces the whole set. Everything the author wrote is gone, so a client showing the message shows a
    /// developer diagnostic where a translated reason should have been.
    /// </summary>
    [Fact] void should_have_displaced_every_authored_rejection() => _results.ShouldNotContain(_ => _.Message == RejectingValidator.NameMessage);
}
