// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventLogInjectionAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventLogInjectionAnalyzer.when_validating_command_handler;

public class and_command_handle_injects_event_log : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSequences;

namespace TestNamespace
{
    [Command]
    public record CreateAuthor(string Name)
    {
        public void Handle(IEventLog {|#0:eventLog|})
        {
        }
    }
}",
                VerifyCS.Diagnostic("ARCCHR0007")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("CreateAuthor", "Handle", "eventLog")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
