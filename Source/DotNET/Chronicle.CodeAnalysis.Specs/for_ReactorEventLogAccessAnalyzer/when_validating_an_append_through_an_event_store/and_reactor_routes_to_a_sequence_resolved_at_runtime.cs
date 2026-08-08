// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_an_append_through_an_event_store;

/// <summary>
/// The analyzer reads the sequence out of the call it can see. A sequence handed in at runtime could be the
/// default log, and following the rule's advice would then be right — but the analyzer cannot tell, and a
/// diagnostic that might be wrong on a legitimate outbox route would break that build for nothing.
/// </summary>
public class and_reactor_routes_to_a_sequence_resolved_at_runtime : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace TestNamespace
{
    public record AuthorRegistered(string Name);

    public class AuthorNotifier(IEventStore eventStore) : IReactor
    {
        public Task On(AuthorRegistered @event, EventSequenceId sequence) =>
            eventStore.GetEventSequence(sequence).Append(""some-author"", @event);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
