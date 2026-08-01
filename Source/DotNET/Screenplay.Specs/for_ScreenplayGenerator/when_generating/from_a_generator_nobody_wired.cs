// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The package ships no dependency injection, so a host that just wants a document has to be able to say
/// <c>new ScreenplayGenerator()</c> and get one. Nothing outside the package should have to know that an analysis
/// half and an emission half exist.
/// </summary>
public class from_a_generator_nobody_wired : given.a_compilation
{
    IScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Establish() => _generator = new ScreenplayGenerator();

    void Because()
    {
        _result = _generator.Generate(_compilation, new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    [Fact] void should_produce_a_document() => _result.Source.ShouldNotBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_without_diagnostics() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_name_the_domain_after_the_compilation() => _result.Model.Domain.ShouldEqual("Library");
    [Fact] void should_not_report_it_as_a_failure() => _result.IsSuccess.ShouldBeTrue();
}
