// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_collected_readers : Specification
{
    CommandScenario<given.a_certified_command.CollectedReaders> _scenario;
    ServiceProvider _root;
    CommandResult _result;
    int _callbacks;

    async Task Establish()
    {
        _scenario = new CommandScenario<given.a_certified_command.CollectedReaders>().UseDecisionReads();
        await _scenario.Validate(new(EventSourceId.New()));
        _scenario.Services.AddProtectedDecisionValidator<given.a_certified_command.CollectedReadersValidator>(_ => _callbacks++);
        _root = (ServiceProvider)_scenario.Services.BuildProtectedDecisionServiceProvider();
    }

    async Task Because() => _result = await _root.GetRequiredService<ICommandPipeline>().Execute(new given.a_certified_command.CollectedReaders(EventSourceId.New()), ValidationResultSeverity.Error);

    async Task Destroy()
    {
        await _root.DisposeAsync();
        await _scenario.DisposeAsync();
    }

    [Fact] void should_refuse_the_collection_instead_of_resolving_superseded_raw_readers() => _result.ExceptionMessages.Single().ShouldContain("Discoverable validator");
    [Fact] void should_not_activate_the_validator() => _callbacks.ShouldEqual(0);
    [Fact] void should_not_append() => _scenario.AppendedEvents.ShouldBeEmpty();
}
