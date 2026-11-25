// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class a_generated_concept_shared_as_metadata : Specification
{
    const string Contracts = """
        using Cratis.Concepts;
        namespace Library.Contracts;
        public record AuthorId(string Value) : ConceptAs<string>(Value), IGeneratable<AuthorId>
        {
            public static AuthorId New() => new(GenerateValue.TypeId("author"));
        }
        """;

    const string Consumer = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Library.Contracts;
        namespace Library.Authors.Listing;
        [ReadModel]
        public record Author(AuthorId AuthorId)
        {
            public static IEnumerable<Author> AllAuthors() => [];
        }
        """;

    Compilation _contracts;
    Compilation _consumer;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _contracts = Analyzed.Project("ZContracts", [], ("Library/Contracts/AuthorId.cs", Contracts));
        _consumer = Analyzed.Project("AApplication", [Analyzed.Package("ZContracts", Contracts)], ("Library/Authors/Listing/Listing.cs", Consumer));
    }

    void Because() => _result = new ScreenplayGenerator().Generate([_contracts, _consumer], new ScreenplayOptions { Domain = "Library", Module = "Library" });

    [Fact] void should_compile_both_projects() => Analyzed.ErrorsIn(_contracts).Concat(Analyzed.ErrorsIn(_consumer)).ShouldBeEmpty();
    [Fact] void should_actually_consume_a_metadata_symbol() => _consumer.GetTypeByMetadataName("Library.Contracts.AuthorId")!.DeclaringSyntaxReferences.ShouldBeEmpty();
    [Fact] void should_declare_the_shared_concept_once() => _result.Model.Concepts.Count(concept => concept.Name == "AuthorId").ShouldEqual(1);
    [Fact] void should_keep_its_source_generation_strategy() => _result.Model.Concepts.Single().Generation!.Kind.ShouldEqual("typeid");
    [Fact] void should_keep_its_source_generation_prefix() => _result.Model.Concepts.Single().Generation!.Prefix.ShouldEqual("author");
    [Fact] void should_emit_the_generation_contract() => _result.Source.ShouldContain("generate typeid \"author\"");
    [Fact] void should_keep_the_read_model_under_its_original_name() => _result.Model.Slices.SelectMany(slice => slice.ReadModels).Single().Name.ShouldEqual("Author");
    [Fact] void should_keep_the_reference_to_the_shared_concept() => _result.Model.Slices.SelectMany(slice => slice.ReadModels).Single().Properties.Single().Type.Name.ShouldEqual("AuthorId");
    [Fact] void should_report_no_false_ambiguity_or_lost_reference() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_bind_the_document() => new ScreenplayVerifier().Verify(_result.Source).BindingDiagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
}
