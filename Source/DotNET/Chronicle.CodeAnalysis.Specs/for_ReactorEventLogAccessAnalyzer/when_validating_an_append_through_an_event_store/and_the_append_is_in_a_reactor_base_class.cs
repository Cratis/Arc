// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_an_append_through_an_event_store;

public class and_the_append_is_in_a_reactor_base_class : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

namespace TestNamespace
{
    public record AuthorRegistered(string Name);

    public abstract class NotifierBase(IEventStore eventStore) : IReactor
    {
        protected Task Record(AuthorRegistered @event) =>
            {|#0:eventStore.EventLog|}.Append(""some-author"", @event);
    }

    public class AuthorNotifier(IEventStore eventStore) : NotifierBase(eventStore)
    {
        public Task On(AuthorRegistered @event) => Record(@event);
    }
}",
                VerifyCS.Diagnostic("ARCCHR0003")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("NotifierBase", "eventStore.EventLog")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
