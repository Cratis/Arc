// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender;

public class when_off_matches_legacy_append_many : Specification
{
    string _actual;
    string _expected;

    async Task Because()
    {
        var (_, log, unit) = DecisionFixtures.Transaction();
        log.Id.Returns(EventSequenceId.Log);
        var events = Array.Empty<EventForEventSourceId>();
        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>();
        var correlation = unit.CorrelationId;
        log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                events = call.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
                scopes = new(call.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3));
                return AppendManyResult.Success(correlation, []);
            });
        var context = new CommandContext(correlation, typeof(LegacyCommand), new LegacyCommand(), [], new());
        var scope = new ConcurrencyScope((EventSequenceNumber)7ul, (EventSourceId)"source");
        var occurred = DateTimeOffset.UnixEpoch;
        CommandTransaction.Current = unit;
        log.TryEnrollForCommand("source", new Fact("value"), context, scope, ["tag"], occurred, new("stream", "instance", "kind", new Subject("subject"), null, null));
        await unit.Commit();
        _actual = JsonSerializer.Serialize(new { events, scopes = scopes.Select(_ => new { Label = _.Key.Value, _.Value }), correlation });

        // Replay today's AddEvent call using the same framework timestamp/causation snapshot.
        // All other arguments are a frozen baseline, not copied from the produced wrapper.
        var causation = events.Single().Causation!;
        var (_, baselineLog, _) = DecisionFixtures.Transaction();
        var baselineStore = Substitute.For<Cratis.Chronicle.IEventStore>();
        baselineStore.GetEventSequence(EventSequenceId.Log).Returns(baselineLog);
        var baseline = new UnitOfWork(correlation, _ => { }, baselineStore);
        baselineLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
                var scopes = call.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
                _expected = JsonSerializer.Serialize(new { events, scopes = scopes.Select(_ => new { Label = _.Key.Value, _.Value }), correlation });
                return AppendManyResult.Success(correlation, []);
            });
        baseline.AddEvent(EventSequenceId.Log, "source", new Fact("value"), causation, "stream", "instance", "kind", scope, ["tag"], occurred, new Subject("subject"));
        await baseline.Commit();
    }

    void Destroy() => CommandTransaction.Current = null;
    [Fact] void should_send_identical_append_arguments() => _actual.ShouldEqual(_expected);

    public record LegacyCommand;
    public record Fact(string Value);
}
