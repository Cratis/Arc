// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_an_append_through_an_event_store;

/// <summary>
/// AppendOperations starts with the same four letters the rule matches append methods by, and reading it
/// writes nothing.
/// </summary>
public class and_reactor_reads_the_append_operations_of_the_event_log : Specification
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
        public object On(AuthorRegistered @event) =>
            eventStore.EventLog.AppendOperations;
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
