// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_automatically_enabling_decision_reads;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_warmed_caches_are_used_concurrently : Specification
{
    readonly EventSourceId _source = EventSourceId.New();
    (IEventStore Store, bool Accepted, bool RejectedDuplicate, int EventCount)[] _results;

    async Task Establish() => await ExerciseScenario();

    async Task Because() => _results = await Task.WhenAll(Task.Run(ExerciseScenario), Task.Run(ExerciseScenario));

    [Fact] void should_keep_distinct_store_names() => _results[0].Store.Name.ShouldNotEqual(_results[1].Store.Name);
    [Fact] void should_keep_distinct_namespaces() => _results[0].Store.Namespace.ShouldNotEqual(_results[1].Store.Namespace);
    [Fact] void should_keep_distinct_stores() => _results[0].Store.ShouldNotBeSame(_results[1].Store);
    [Fact] void should_accept_the_same_unique_value_in_each_store() => _results.All(_ => _.Accepted).ShouldBeTrue();
    [Fact] void should_enforce_uniqueness_within_each_store() => _results.All(_ => _.RejectedDuplicate).ShouldBeTrue();
    [Fact] void should_not_share_or_duplicate_events() => _results.All(_ => _.EventCount == 1).ShouldBeTrue();

    async Task<(IEventStore Store, bool Accepted, bool RejectedDuplicate, int EventCount)> ExerciseScenario()
    {
        await using var scenario = new CommandScenario<AutomaticRead>();
        (await scenario.Validate(new AutomaticRead(_source))).IsSuccess.ShouldBeTrue();
        var store = (IEventStore)scenario.Services.Single(_ => _.ServiceType == typeof(IEventStore)).ImplementationInstance!;
        var accepted = await scenario.EventLog.Append(_source, new PartnerOnboardingStarted("ORG-CACHED"));
        var duplicate = await scenario.EventLog.Append(EventSourceId.New(), new PartnerOnboardingStarted("ORG-CACHED"));
        var events = await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        return (store, accepted.IsSuccess, !duplicate.IsSuccess, events.Count);
    }
}
