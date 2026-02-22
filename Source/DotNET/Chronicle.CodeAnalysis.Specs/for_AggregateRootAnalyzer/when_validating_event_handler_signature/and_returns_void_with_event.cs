// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.AggregateRootAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_AggregateRootAnalyzer.when_validating_event_handler_signature;

public class and_returns_void_with_event : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Chronicle.Aggregates;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestEvent { }
    
    public class TestAggregate : AggregateRoot
    {
        public void OnTestEvent(TestEvent e)
        {
        }
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
