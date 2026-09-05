// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

/// <summary>
/// The other side of <see cref="when_a_validator_throws"/>, and the reason the discriminator is worth anything: an
/// authored rejection has to stay indistinguishable from what it always was. If a rule rejection were also marked
/// as something the framework composed, a client branching on the reason would stop showing authored messages.
/// </summary>
public class when_a_validator_rejects : given.a_validator_invoker
{
    IEnumerable<ValidationResult> _results;

    async Task Because() => _results = await _invoker.Invoke(
        new Subject("anything", "anything"),
        new RejectingValidator(),
        string.Empty);

    [Fact] void should_return_every_authored_rejection() => _results.Count().ShouldEqual(2);
    [Fact] void should_keep_the_authored_messages() => _results.Select(_ => _.Message).ShouldContainOnly(RejectingValidator.NameMessage, RejectingValidator.EmailMessage);
    [Fact] void should_say_the_rejections_came_from_authored_rules() => _results.Select(_ => _.Reason).ShouldContainOnly(ValidationResultReason.Rule, ValidationResultReason.Rule);
    [Fact] void should_attribute_them_to_their_members() => _results.SelectMany(_ => _.Members).ShouldContainOnly("name", "email");
    [Fact] void should_leave_the_authors_own_state_alone() => _results.Single(_ => _.Members.Contains("name")).State.ShouldEqual(RejectingValidator.AuthorState);
}
