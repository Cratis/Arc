// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_an_append_through_an_event_store;

/// <summary>
/// A local could hold the reactor's own store or one obtained from IChronicleClient, and the analyzer does
/// not follow values through locals to tell them apart. It stays quiet rather than risk telling an author to
/// return an event that would land in a different store — a miss here costs less than that false positive.
/// </summary>
public class and_reactor_appends_through_a_local_holding_the_event_store : Specification
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
        public Task On(AuthorRegistered @event)
        {
            var store = eventStore;
            return store.EventLog.Append(""some-author"", @event);
        }
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
