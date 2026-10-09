// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_deciding;

public class with_a_definition_route : given.a_stream_decision
{
    CommandScenario<DecideDefinition> _definitionScenario;
    DecideDefinition _command;
    EventRoute _resolved;

    void Establish()
    {
        _definitionScenario = new CommandScenario<DecideDefinition>().UseDecisionReads();
        _command = new(_id, new("owner"), "month");
        _resolved = _definitionScenario.EventRoutes.For<ReportSource>("approval").WithStreamId(EventStreamIdTemplate.ResolveFor(_command)!);
        _definitionScenario.Given.ForEventSource(_id).OnRoute(_resolved).Events(new PriorFact("prior"));
    }

    async Task Because() => _result = await _definitionScenario.Execute(_command);
    async Task Destroy() => await _definitionScenario.DisposeAsync();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_read_the_seeded_route() => ((Approved)_definitionScenario.AppendedEvents.Single().Event.Content).PriorCount.ShouldEqual(1);
    [Fact] void should_append_to_the_resolved_stream_id() => _definitionScenario.AppendedEvents.Single().Event.Context.EventStreamId.ShouldEqual(_resolved.EventStreamId);
    [Fact] void should_keep_the_definition_name() => _definitionScenario.AppendedEvents.Single().Event.Context.EventSource.Value.ShouldEqual("reports");
    [Fact] void should_match_attribute_resolution()
    {
        var idValues = new EventStreamIdValuesProvider().Provide(_command);
        var definitions = new Cratis.Chronicle.EventSources.EventSourceDefinition(typeof(ReportSource), "reports", "", _resolved.Concurrency, [new("approval", "", _resolved.Concurrency)]);
        var sources = Substitute.For<Cratis.Chronicle.EventSources.IEventSources>();
        sources.GetFor(typeof(ReportSource)).Returns(definitions);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(Cratis.Chronicle.EventSources.IEventSources)).Returns(sources);
        var values = new EventSourceDefinitionValuesProvider(services).Provide(_command);
        foreach (var pair in idValues)
        {
            values.Add(pair.Key, pair.Value);
        }
        var context = new CommandContext(Cratis.Execution.CorrelationId.New(), typeof(DecideDefinition), _command, [], values, null);
        context.GetEventRoute().ShouldEqual(_resolved);
    }
}
