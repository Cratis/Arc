// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class unused_declarations_sharing_names : a_generated_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Validation;
        using FluentValidation;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace A.Unused
        {
            public record Identifier(int Value) : ConceptAs<int>(Value);
            public class IdentifierValidator : ConceptValidator<Identifier>
            {
                public IdentifierValidator() { RuleFor(value => value.Value).GreaterThan(0); }
            }
            public record Details(int Value) : ConceptAs<int>(Value);
        }
        namespace C.Unused
        {
            public record Token(int Value) : ConceptAs<int>(Value);
        }
        namespace B.Unused
        {
            public record Token(string Value) : ConceptAs<string>(Value);
        }
        namespace Library.Authors.Registration
        {
            public record Identifier(string Value) : ConceptAs<string>(Value);
            public class IdentifierValidator : ConceptValidator<Identifier>
            {
                public IdentifierValidator() { RuleFor(value => value.Value).NotEmpty(); }
            }
            public record Details(string Name);
            [EventType]
            public record AuthorRegistered(Identifier Id);
            [ReadModel]
            public record Entry(string EntryId, Identifier Id, Details Detail)
            {
                public static IEnumerable<Entry> AllEntries() => [];
            }
        }
        """;

    void Because() => Generate((Analyzed.SlicePath, Source));

    [Fact] void should_keep_the_referenced_concepts_primitive() => Result.Model.Concepts.Single(concept => concept.Name == "Identifier").Primitive.ShouldEqual(ScreenplayPrimitive.String);
    [Fact] void should_declare_the_referenced_concept_once() => Result.Model.Concepts.Count(concept => concept.Name == "Identifier").ShouldEqual(1);
    [Fact] void should_keep_the_referenced_object_shape() => Result.Model.Types.Single(type => type.Name == "Details").Properties.Single().Name.ShouldEqual("Name");
    [Fact] void should_not_replace_the_object_with_an_unused_concept() => Result.Model.Concepts.Any(concept => concept.Name == "Details").ShouldBeFalse();
    [Fact] void should_keep_the_read_model_under_its_original_name() => Result.Model.Slices.SelectMany(slice => slice.ReadModels).Single().Name.ShouldEqual("Entry");
    [Fact] void should_choose_the_first_unused_declaration_by_qualified_name() => Result.Model.Concepts.Single(concept => concept.Name == "Token").Primitive.ShouldEqual(ScreenplayPrimitive.String);
    [Fact] void should_declare_the_unused_name_once() => Result.Model.Concepts.Count(concept => concept.Name == "Token").ShouldEqual(1);
    [Fact] void should_keep_only_the_referenced_concepts_validator() => Result.Model.Concepts.Single(concept => concept.Name == "Identifier").Validations.Single().Kind.ShouldEqual(ValidationRuleKind.NotEmpty);
    [Fact] void should_report_no_false_ambiguity_or_lost_reference() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_bind_the_unchanged_references() => AssertDocument();
}
