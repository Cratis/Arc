// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.MissingWithChronicleAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_MissingWithChronicleAnalyzer.when_setting_up_arc;

public class and_a_command_injects_the_event_log_without_wiring_chronicle : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.EventSequences;

namespace TestNamespace
{
    public static class Setup
    {
        public static object AddCratisArc(this object builder) => builder;
    }

    public record RegisterAuthor(string Name)
    {
        public void Handle(IEventLog eventLog)
        {
        }
    }

    public static class Program
    {
        public static void Configure(object builder) => {|#0:builder.AddCratisArc()|};
    }
}",
        VerifyCS.Diagnostic("ARCCHR0005")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("RegisterAuthor")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
