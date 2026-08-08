// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_an_append_through_an_event_store;

/// <summary>
/// The reported source text goes into a message the CLI, SARIF readers, and IDE error lists all render on a
/// single line, so the access has to arrive there without the line breaks and indentation it was written with.
/// </summary>
public class and_reactor_appends_through_a_multi_line_fluent_call : Specification
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
        public Task On(AuthorRegistered @event) =>
            {|#0:eventStore
                .GetEventSequence(EventSequenceId.Log)|}
                .Append(""some-author"", @event);
    }
}",
                VerifyCS.Diagnostic("ARCCHR0003")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("eventStore.GetEventSequence(EventSequenceId.Log)")));

    [Fact] void should_report_the_access_on_a_single_line() => _result.ShouldBeNull();
}
