// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class with_an_executable_model_cap : Specification
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
        .Generate(CSharpCompilation.Create("Library"), new("Library") { MaximumExecutableModelVersion = SemanticVersion.V6 });

    [Fact] void should_carry_the_cap_into_analysis() => _analyzedOptions.MaximumExecutableModelVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_withhold_generated_values_in_every_scope() => _result.Documents.All(document => !document.Source.Contains("generated", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_withhold_responses_in_every_scope() => _result.Documents.All(document => !document.Source.Contains("returns", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_cap_with_sp0052() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("ESM v6.0", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_cap_when_resolving_defaults() => new EmbeddedDocumentOptions("Library") { MaximumExecutableModelVersion = SemanticVersion.V6 }.Resolve().MaximumExecutableModelVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_have_no_cap_by_default() => new EmbeddedDocumentOptions("Library").MaximumExecutableModelVersion.ShouldBeNull();
    [Fact] void should_bind_the_root_below_v7()
    {
        var source = _result.Documents.Single(document => document.Document.Kind == EmbeddedDocumentKind.Assembly).Source;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        var bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        bound.Success.ShouldBeTrue();
        SemanticVersion.V6.IsAtLeast(bound.Value!.Model.SemanticVersion).ShouldBeTrue();
    }
}
