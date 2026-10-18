// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_a_command_aware_reader : Specification
{
    CommandScenario<ReadInRule> _scenario;
    ServiceProvider _root;
    CommandResult _result;
    ReadInRule _command;

    async Task Establish()
    {
        _scenario = new CommandScenario<ReadInRule>().UseDecisionReads();
        _command = new(EventSourceId.New());
        await _scenario.Validate(_command);
        _scenario.AppendConcurrently(_command.EventSourceId, new when_using_decision_mode.DecisionStateChanged());
        _root = (ServiceProvider)_scenario.Services.BuildProtectedDecisionServiceProvider();
    }

    async Task Because() => _result = await _root.GetRequiredService<ICommandPipeline>().Execute(_command);

    async Task Destroy()
    {
        await _root.DisposeAsync();
        await _scenario.DisposeAsync();
    }

    [Fact] void should_enroll_reads_made_through_the_injected_command_reader() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_not_commit_an_owner_event() => _scenario.AppendedEvents.ShouldBeEmpty();

    [Command]
    [ProtectedDecision]
    public record ReadInRule(EventSourceId EventSourceId)
    {
        public when_using_decision_mode.DecisionFinished Handle() => new(false);
    }

    public class ReaderValidator : CommandValidator<ReadInRule>
    {
        public ReaderValidator(IDecisionReads reads) => RuleFor(command => command.EventSourceId).MustAsync(async (source, cancellationToken) =>
        {
            await reads.Get<when_using_decision_mode.DecisionState>((ReadModelKey)source, cancellationToken);
            return true;
        });
    }
}
