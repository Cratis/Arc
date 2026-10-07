// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class with_a_concept_exercised_in_another_scope : Specification
{
    ApplicationModel _model;
    EmbeddedDocumentGeneration _result;

    void Establish()
    {
        var command = new CommandModel("RegisterAuthor", null, [new("Name", new("AuthorName", false, false))], null, [], [], null, null);
        var registration = SliceModel.Empty("Library.Authors.Registration", "Registration", SliceKind.StateChange) with
        {
            Commands = [command],
            Specifications = [new("ANameIsRequired", [], new("RegisterAuthor", SpecificationStateKind.Command, [new("Name", new LiteralSource(""))]), [], ["name-required"])]
        };
        var editing = SliceModel.Empty("Library.Editors.Editing", "Editing", SliceKind.StateChange) with
        {
            Commands = [command with { Name = "EditAuthor" }]
        };
        var concept = new ConceptModel("AuthorName", ScreenplayPrimitive.String, false, [], [new("Value", ValidationRuleKind.NotEmpty, null, null), new("Value", ValidationRuleKind.Rule, "IsKnownName", "Use a known name") { SourceFilePath = "Authors/Names.cs" }]);
        _model = new("Library", "Library", [concept], [], [registration, editing], []);
    }

    void Because() => _result = new EmbeddedDocumentGenerator().Generate(_model, new("Library", "Library"), []);

    [Fact] void should_generate_both_scopes() => _result.Documents.Select(document => document.Document.Id).ShouldContain("Library.Editors");
    [Fact] void should_generate_the_exercised_scope() => _result.Documents.Select(document => document.Document.Id).ShouldContain("Library.Authors");
    [Fact] void should_withhold_the_concept_rule_in_every_document_including_the_assembly() => _result.Documents.All(document => !document.Source.Contains("IsKnownName", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_declarative_concept_validation_in_every_document() => _result.Documents.All(document => document.Source.Contains("validate", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_withheld_rule_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldEqual(1);
    [Fact] void should_compile_every_document() => _result.Documents.All(document => new ScreenplayCompiler().Compile(document.Source).Success).ShouldBeTrue();
}
