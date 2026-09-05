// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_an_append_through_an_event_store;

/// <summary>
/// A base class that does not implement IReactor is not one, and the analyzer sees only the type the append
/// sits in — it cannot know some derived type elsewhere in the compilation turns it into a reactor. The
/// violation survives this shape; deriving a reactor from a base that appends is the way around the rule.
/// </summary>
public class and_the_append_is_in_a_base_class_that_is_not_a_reactor : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

namespace TestNamespace
{
    public record AuthorRegistered(string Name);

    public abstract class AppenderBase(IEventStore eventStore)
    {
        protected Task Record(AuthorRegistered @event) => eventStore.EventLog.Append(""some-author"", @event);
    }

    public class AuthorNotifier(IEventStore eventStore) : AppenderBase(eventStore), IReactor
    {
        public Task On(AuthorRegistered @event) => Record(@event);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
