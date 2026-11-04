// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Verification;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class and_a_condition_has_a_mismatched_enumeration_operand : for_UnboundProductionAdmission.given.an_unbound_command
{
    ScreenplayGenerationResult _result;
    ScreenplayGenerationResult _authoring;

    void Because()
    {
        _result = Generate(Options);
        _authoring = Generate(Options with { AuthoringOnlyConstructs = true });
    }

    ScreenplayGenerationResult Generate(ScreenplayOptions options)
    {
        var analyzer = Substitute.For<IApplicationModelAnalyzer>();
        analyzer.Analyze(Arg.Any<IReadOnlyList<Compilation>>(), Arg.Any<ScreenplayOptions>()).Returns(ApplicationModelAnalysis.For(Model));
        return new ScreenplayGenerator(analyzer, Emitter).Generate(Analyzed.Compile((Analyzed.SlicePath, "namespace Library;")), options);
    }

    [Fact] void should_keep_the_original_analysis() => _result.Model.ShouldEqual(Model);
    [Fact] void should_leave_the_command_in_code() => _result.Source.ShouldContain("handler\n");
    [Fact] void should_withhold_the_invalid_condition() => _result.Source.ShouldNotContain("role == 2");
    [Fact] void should_bind_the_admitted_document() => new ScreenplayVerifier().Verify(_result.Source).UnexpectedBindingErrors().ShouldBeEmpty();
    [Fact] void should_report_the_original_error_as_a_loss() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).Message.ShouldContain("PLAY0268");
    [Fact] void should_not_report_sp0056_as_an_error_for_the_admitted_document() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_report_sp0056_as_a_warning() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);

    [Fact]
    void should_not_emit_the_invalid_condition_in_authoring_mode()
    {
        _authoring.Source.ShouldNotContain("role == 2");
        new ScreenplayVerifier().Verify(_authoring.Source).UnexpectedBindingErrors(true).ShouldBeEmpty();
    }
}
