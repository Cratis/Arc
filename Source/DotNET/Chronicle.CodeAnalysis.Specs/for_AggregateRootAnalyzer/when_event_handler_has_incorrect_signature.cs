// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.AggregateRootAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_AggregateRootAnalyzer;

public class when_event_handler_has_incorrect_signature
{
    [Fact] void should_report_diagnostic_for_wrong_return_type() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Chronicle.Aggregates;

namespace TestNamespace
{
    public class TestEvent { }
    
    public class TestAggregate : AggregateRoot
    {
        public {|#0:string OnTestEvent(TestEvent e)|}
        {
            return ""invalid"";
        }
    }
}",
        VerifyCS.Diagnostic("ARCCHR0001")
            .WithLocation(0)
            .WithArguments("OnTestEvent", "TestAggregate", "string OnTestEvent(TestNamespace.TestEvent e)")).Wait();

    [Fact] void should_report_diagnostic_for_task_with_result() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Chronicle.Aggregates;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestEvent { }
    
    public class TestAggregate : AggregateRoot
    {
        public {|#0:Task<int> OnTestEvent(TestEvent e)|}
        {
            return Task.FromResult(42);
        }
    }
}",
        VerifyCS.Diagnostic("ARCCHR0001")
            .WithLocation(0)
            .WithArguments("OnTestEvent", "TestAggregate", "System.Threading.Tasks.Task<int> OnTestEvent(TestNamespace.TestEvent e)")).Wait();
}
