// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorEventLogAccessAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorEventLogAccessAnalyzer.when_validating_reactor_dependencies;

/// <summary>
/// An interface of the application's own on top of IEventLog reaches the same sequence, so naming it in a
/// reactor's constructor is the same violation as naming IEventLog.
/// </summary>
public class and_reactor_injects_a_derived_event_log : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace TestNamespace
{
    public interface IAuthorEventLog : IEventLog;

    public class AuthorNotifier(IAuthorEventLog {|#0:eventLog|}) : IReactor
    {
    }
}",
                VerifyCS.Diagnostic("ARCCHR0003")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("AuthorNotifier", "eventLog")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
