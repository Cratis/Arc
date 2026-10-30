// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_stage_shaped_arc_policies : Specification
{
    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(StageShapedPolicySources.All()), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    bool HasLine(string text) => _result.Source.Split('\n').Any(_ => string.Equals(_.Trim(), text, StringComparison.Ordinal));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(StageShapedPolicySources.All()).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_recover_the_accountant_condition() => HasLine("require authenticated and role \"Accountant\"").ShouldBeTrue();
    [Fact] void should_recover_the_automation_condition_without_adding_authentication() => HasLine("require role \"Automation\"").ShouldBeTrue();
    [Fact] void should_report_no_unrecoverable_policy() => _result.Diagnostics.Where(_ => _.Code == "SP0026").ShouldBeEmpty();
}
