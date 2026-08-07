// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogInjectionAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogInjectionAnalyzer.when_validating_reactor_dependencies;

public class and_non_reactor_appends_through_event_store_event_log : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle;

namespace TestNamespace
{
    public record AuthorRegistered(string Name);

    public class SomeService(IEventStore eventStore)
    {
        public Task Register(AuthorRegistered @event) =>
            eventStore.EventLog.Append(""some-author"", @event);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
