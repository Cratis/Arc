// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_an_append_through_an_event_store;

/// <summary>
/// The rule is about what a reactor does, and it decides that from the type the append sits in. A helper type
/// nested inside a reactor is not itself one, so the append is left alone — the same answer the analyzer gives
/// for any other collaborator the reactor delegates to.
/// </summary>
public class and_the_append_is_in_a_nested_type : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

namespace TestNamespace
{
    public record AuthorRegistered(string Name);

    public class AuthorNotifier(IEventStore eventStore) : IReactor
    {
        public Task On(AuthorRegistered @event) => new Appender(eventStore).Record(@event);

        class Appender(IEventStore inner)
        {
            public Task Record(AuthorRegistered @event) => inner.EventLog.Append(""some-author"", @event);
        }
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
