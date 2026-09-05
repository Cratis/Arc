// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventLogInjectionAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventLogInjectionAnalyzer.when_validating_command_handler;

public class and_non_command_injects_event_log : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.EventSequences;

namespace TestNamespace
{
    public class SomeService
    {
        public void Handle(IEventLog eventLog)
        {
        }
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
