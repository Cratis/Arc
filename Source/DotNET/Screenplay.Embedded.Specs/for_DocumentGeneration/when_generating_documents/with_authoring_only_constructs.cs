// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class with_authoring_only_constructs : Specification
{
    IApplicationModelAnalyzer _analyzer;
    EmbeddedDocumentGeneration _result;
    ScreenplayOptions _analyzedOptions;

    void Establish()
    {
        var command = new CommandModel("RegisterAuthor", null, [new("Name", new("String", false, false))], null, [], [], null, null)
        {
            Authoring = new()
            {
                Generated = [new("authorId", new("AuthorId", false, false))],
                Identifier = "authorId",
                Response = "authorId"
            }
        };
        var slice = SliceModel.Empty("Library.Authors.Registration", "Registration", SliceKind.StateChange) with { Commands = [command] };
        var model = new ApplicationModel("Library", "Library", [new("AuthorId", ScreenplayPrimitive.Uuid, false, [], [])], [], [slice], []);
        _analyzer = Substitute.For<IApplicationModelAnalyzer>();
        _analyzer.Analyze(Arg.Any<IReadOnlyList<Compilation>>(), Arg.Do<ScreenplayOptions>(options => _analyzedOptions = options))
            .Returns(new ApplicationModelAnalysis(model, []));
    }

    void Because() => _result = new EmbeddedDocumentGenerator(_analyzer, new ScreenplayEmitter(), new ScreenplayVerifier())
        .Generate(CSharpCompilation.Create("Library"), new("Library") { AuthoringOnlyConstructs = true });

    [Fact] void should_carry_the_option_into_analysis() => _analyzedOptions.AuthoringOnlyConstructs.ShouldBeTrue();
    [Fact] void should_emit_the_generated_response_in_every_scope() => _result.Documents.All(document => document.Source.Contains("returns authorId", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_every_embedded_document_without_errors_or_warnings() => _result.Documents.SelectMany(document => new ScreenplayCompiler().Compile(document.Source).Diagnostics)
        .Where(diagnostic => diagnostic.Severity is Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error or Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_preserve_the_option_when_resolving_defaults() => new EmbeddedDocumentOptions("Library") { AuthoringOnlyConstructs = true }.Resolve().AuthoringOnlyConstructs.ShouldBeTrue();
    [Fact] void should_leave_the_option_disabled_by_default() => new EmbeddedDocumentOptions("Library").AuthoringOnlyConstructs.ShouldBeFalse();
}
