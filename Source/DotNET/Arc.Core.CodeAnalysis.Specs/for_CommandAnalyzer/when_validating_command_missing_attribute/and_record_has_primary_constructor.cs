// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer.when_validating_command_missing_attribute;

public class and_record_has_primary_constructor : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
namespace TestNamespace
{
    public record {|#0:TestCommand|}(string Name, int Age)
    {
        public void Handle()
        {
        }
    }
}",
                VerifyCS.Diagnostic("ARC0002")
                    .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("TestCommand")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
