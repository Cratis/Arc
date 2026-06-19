// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogInjectionAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogInjectionAnalyzer.when_validating_reactor_dependencies;

public class and_reactor_injects_event_log : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.EventSequences;

namespace TestNamespace
{
    public class AuthorNotifier(IEventLog {|#0:eventLog|}) : IReactor
    {
    }
}",
                VerifyCS.Diagnostic("ARCCHR0003")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("AuthorNotifier", "eventLog")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
