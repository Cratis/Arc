// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.AggregateRootAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_AggregateRootAnalyzer;

public class when_event_handler_has_correct_signature
{
    [Fact] void should_not_report_diagnostic_for_void_with_event() => VerifyCS.VerifyAnalyzerAsync(@"
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
}").Wait();

    [Fact] void should_not_report_diagnostic_for_task_with_event() => VerifyCS.VerifyAnalyzerAsync(@"
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
}").Wait();

    [Fact] void should_not_report_diagnostic_for_void_with_event_and_context() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestEvent { }
    
    public class TestAggregate : AggregateRoot
    {
        public void OnTestEvent(TestEvent e, EventContext context)
        {
        }
    }
}").Wait();
}
