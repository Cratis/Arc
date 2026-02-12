// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.AggregateRootAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_AggregateRootAnalyzer.when_validating_event_handler_signature;

public class and_returns_task_with_event : Specification
{
    Exception result;

    async Task Because()
    {
        try
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Chronicle.Aggregates;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestEvent { }
    
    public class TestAggregate : AggregateRoot
    {
        public Task OnTestEvent(TestEvent e)
        {
            return Task.CompletedTask;
        }
    }
}");
        }
        catch (Exception ex)
        {
            result = ex;
        }
    }

    [Fact] void should_not_report_diagnostic() => result.ShouldBeNull();
}
