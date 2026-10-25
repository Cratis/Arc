// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_no_appended_events;

public class and_only_a_failed_append_happened : given.an_appended_event_assertion
{
    async Task Because()
    {
        _result = await _scenario.Execute(new(_source, []));
        var capture = (List<AppendedEventWithResult>)_scenario.Context[ChronicleCommandScenarioExtender.AppendedEventsKey];

        // A failed append receipt has no committed event; every assertion must filter it before reading Content.
        capture.Add(new(null!, AppendResult.Failed(CorrelationId.New(), [new AppendError("rejected")])));
        _error = await Catch.Exception(_scenario.ShouldNotHaveAppendedEvents);
    }

    [Fact] void should_ignore_a_failed_append_receipt() => _error.ShouldBeNull();
    [Fact] async Task should_ignore_failed_receipts_when_checking_a_source() => await _scenario.ShouldNotHaveAppendedEvents(_source);
    [Fact] async Task should_ignore_failed_receipts_in_ordered_comparison() => await _scenario.ShouldHaveAppendedEventsInOrder();
    [Fact] async Task should_ignore_failed_receipts_in_unordered_comparison() => await _scenario.ShouldHaveAppendedEventsInAnyOrder();
}
