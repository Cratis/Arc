// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_approval_is_required_without_a_flow : given.an_evaluation
{
    CommandResult _execution;
    CommandResult _validation;

    async Task Because()
    {
        Result = await CommandAuthorization.For<given.ApprovalCommand>(Provider, Principal);
        await using var scenario = new CommandScenario<given.ApprovalCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(Principal);
        _execution = await scenario.Execute(new given.ApprovalCommand());
        _validation = await scenario.Validate(new given.ApprovalCommand());
    }

    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_approval() => Result.ReasonCode.ShouldEqual("approval-required");
    [Fact] void should_refuse_pipeline_execution() => _execution.IsAuthorized.ShouldBeFalse();
    [Fact] void should_refuse_pipeline_validation() => _validation.IsAuthorized.ShouldBeFalse();
    [Fact] void should_explain_the_pipeline_refusal() => _execution.AuthorizationFailureReason.ShouldContain("Approval required");
    [Fact] void should_not_handle() => Behavior.HandlerCalls.ShouldEqual(0);
    [Fact] async Task should_also_reject_an_instance() => (await CommandAuthorization.For(Provider, Principal, new given.ApprovalCommand(), TestContext.Current.CancellationToken)).ReasonCode.ShouldEqual("approval-required");
    [Fact] async Task should_honor_inherited_requirements() => (await CommandAuthorization.For<given.InheritedApprovalCommand>(Provider, Principal, TestContext.Current.CancellationToken)).ReasonCode.ShouldEqual("approval-required");
}
