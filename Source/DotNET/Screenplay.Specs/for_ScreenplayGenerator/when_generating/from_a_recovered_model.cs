// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The generator is the two halves joined together. It resolves the options once, hands them to analysis, emits what
/// analysis recovered, and reports everything either half could not express.
/// </summary>
public class from_a_recovered_model : given.a_compilation
{
    given.a_recovered_model _analyzer;
    ScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _analyzer = new(
            LibraryApplication.Build(),
            new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP9999", "Something was not recovered", "Library"));
        _generator = new(_analyzer, new ScreenplayEmitter());
    }

    void Because() => _result = _generator.Generate(_compilation, new ScreenplayOptions());

    [Fact] void should_return_the_model_it_generated_from() => _result.Model.Domain.ShouldEqual("Library");
    [Fact] void should_print_the_document() => _result.Source.StartsWith("domain Library", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_carry_forward_what_analysis_reported() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly(["SP9999"]);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_resolve_the_domain_from_the_compilation() => _analyzer.Options!.Domain.ShouldEqual("Library");
    [Fact] void should_default_the_module_to_the_domain() => _analyzer.Options!.Module.ShouldEqual("Library");
    [Fact] void should_default_the_segments_to_skip() => _analyzer.Options!.SegmentsToSkip.ShouldEqual(0);
}
