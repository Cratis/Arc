// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Testing.EventSequences;
using Microsoft.Extensions.DependencyInjection;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_automatically_enabling_decision_reads;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_provisioning_decision_reads;

public class automatically : Specification
{
    CommandScenario<AutomaticRead> _scenario;
    CommandScenario<Ordinary> _explicit;
    IEventStore _store;
    IEventStore _firstStore;
    Exception _facadeError;

    void Establish()
    {
        _scenario = new CommandScenario<AutomaticRead>();
        _explicit = new CommandScenario<Ordinary>().UseDecisionReads();
    }

    void Because()
    {
        _store = Stores(_scenario.Services).Single();
        _facadeError = Catch.Exception(() => _ = _scenario.EventScenario);
        _firstStore = _store;
        _scenario.UseDecisionReads();
    }

    void Destroy()
    {
        _scenario.Dispose();
        _explicit.Dispose();
    }

    [Fact] void should_not_register_an_event_scenario_context_entry() => _scenario.Context.ContainsKey(ChronicleCommandScenarioExtender.ContextKey).ShouldBeFalse();
    [Fact] void should_not_hold_an_event_scenario() => _scenario.Context.Values.OfType<EventScenario>().ShouldBeEmpty();
    [Fact] void should_register_exactly_one_event_store() => Stores(_scenario.Services).Count().ShouldEqual(1);
    [Fact] void should_name_its_store_uniquely() => _store.Name.Value.StartsWith("test-event-store-").ShouldBeTrue();
    [Fact] void should_name_its_namespace_uniquely() => _store.Namespace.Value.StartsWith("default-").ShouldBeTrue();
    [Fact] void should_register_the_same_services_as_the_explicit_opt_in() => string.Join(Environment.NewLine, Signatures(_scenario.Services)).ShouldEqual(string.Join(Environment.NewLine, Signatures(_explicit.Services)));
    [Fact] void should_hold_the_same_context_keys_as_the_explicit_opt_in_without_the_event_scenario() =>
        string.Join(Environment.NewLine, _scenario.Context.Keys.Order()).ShouldEqual(string.Join(Environment.NewLine, _explicit.Context.Keys.Where(_ => _ != ChronicleCommandScenarioExtender.ContextKey).Order()));
    [Fact] void should_refuse_the_event_scenario_facade() => _facadeError.ShouldBeOfExactType<EventScenarioUnavailableInDecisionMode>();
    [Fact] void should_keep_the_store_when_opting_in_again() => Stores(_scenario.Services).Single().ShouldEqual(_firstStore);

    static IEnumerable<IEventStore> Stores(IServiceCollection services) =>
        services
            .Where(_ => _.ServiceType == typeof(IEventStore))
            .Select(_ => (IEventStore)_.ImplementationInstance!);

    static IEnumerable<string> Signatures(IServiceCollection services) =>
        services
            .Select((_, index) => (Index: index, Descriptor: _))
            .OrderBy(_ => _.Descriptor.ServiceType.ToString(), StringComparer.Ordinal)
            .ThenBy(_ => _.Index)
            .Select(_ => _.Descriptor)
            .Select(_ => $"{_.ServiceType}|{_.Lifetime}|{_.IsKeyedService}|{_.ImplementationType}|{_.ImplementationInstance?.GetType()}|{_.ImplementationFactory is not null}");
}
