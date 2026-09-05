// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// A suppressed emission must not move the delta baseline. The client never saw it, so the next delivered
/// <see cref="ChangeSet"/> has to be computed against the last state it actually received — otherwise the changes that
/// happened while the guard was withholding are folded into "already delivered" and vanish, with no error anywhere.
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
    void should_report_everything_that_changed_since_the_last_delivered_emission() =>
        QueryResultsFor(FirstQueryId)[1].ChangeSet!.Added.Count().ShouldEqual(2);

    [Fact]
    void should_not_report_anything_as_removed() =>
        QueryResultsFor(FirstQueryId)[1].ChangeSet!.Removed.Count().ShouldEqual(0);
}
