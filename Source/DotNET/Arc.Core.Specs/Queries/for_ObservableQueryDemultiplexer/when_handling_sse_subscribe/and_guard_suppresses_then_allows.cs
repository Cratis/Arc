// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// A suppressed emission must not advance the delivered state. For collections without identity, the next
/// allowed emission must contain the complete current snapshot, including changes made while the guard withheld data.
/// </summary>
public class and_guard_suppresses_then_allows : given.a_guarded_sse_connection
{
    void Establish() =>
        _verdict = _ => _guardCalls.Count == 2
            ? ObservableQueryEmissionVerdict.Suppress
            : ObservableQueryEmissionVerdict.Allow;

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["item-a"]);
        await WaitFor(() => _guardCalls.Count == 1);

        _subject.OnNext(["item-a", "item-b"]);
        await WaitFor(() => _guardCalls.Count == 2);

        _subject.OnNext(["item-a", "item-b", "item-c"]);
        await WaitFor(() => _guardCalls.Count == 3);
        await WaitFor(() => QueryResultsFor(FirstQueryId).Count == 2);
    });

    [Fact] void should_withhold_exactly_one_emission() => QueryResultsFor(FirstQueryId).Count.ShouldEqual(2);

    [Fact]
    void should_send_the_complete_current_snapshot_after_suppression() =>
        ((System.Text.Json.JsonElement)QueryResultsFor(FirstQueryId)[1].Data).GetArrayLength().ShouldEqual(3);

    [Fact]
    void should_not_send_a_change_set() =>
        QueryResultsFor(FirstQueryId)[1].ChangeSet.ShouldBeNull();
}
