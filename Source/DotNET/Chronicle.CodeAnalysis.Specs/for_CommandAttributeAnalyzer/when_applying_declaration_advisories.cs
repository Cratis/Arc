// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_applying_declaration_advisories
{
    [Theory]
    [InlineData("abstract class")]
    [InlineData("class")]
    public Task should_allow_inherited_read_protection_on_a_base_without_handle(string declaration) =>
        AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
            $"using Cratis.Arc.Commands; [ReadProtection(ReadProtectionMode.On)] public {declaration} Base; public class Command : Base {{ public void Handle() {{ }} }}");

    [Fact]
    public Task should_report_off_when_the_assembly_is_already_off() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; [ReadProtection(ReadProtectionMode.Off)] public class Command { public void Handle() { } }",
        new ExpectedDiagnostic("ARCCHR0032", DiagnosticSeverity.Info, "assembly is already Off"));

    [Fact]
    public Task should_not_offer_an_unrelated_property_for_a_distant_name() => CodeFixVerifier<CommandAttributeAnalyzer, UseNameofForLookUpByCodeFixProvider>.VerifyNoCodeFixAsync(
        "using Cratis.Arc.Commands; public class Command { public string Id { get; set; } = string.Empty; public void Handle([LookUpBy(\"CustomerKey\")] Model model) { } } public record Model(string Id);",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "CustomerKey"));

    [Fact]
    public Task should_suggest_a_close_name_whose_key_is_converted_at_runtime() => CodeFixVerifier<CommandAttributeAnalyzer, UseNameofForLookUpByCodeFixProvider>.VerifyCodeFixAsync(
        "using Cratis.Arc.Commands; public class Command { public int Customer { get; set; } public void Handle([LookUpBy(\"Custome\")] Model model) { } } public record Model(string Id);",
        "using Cratis.Arc.Commands; public class Command { public int Customer { get; set; } public void Handle([LookUpBy(nameof(global::Command.Customer))] Model model) { } } public record Model(string Id);",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "Custome"));
}
