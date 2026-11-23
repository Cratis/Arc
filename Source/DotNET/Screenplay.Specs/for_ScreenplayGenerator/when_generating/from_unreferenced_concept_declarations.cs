// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unreferenced_concept_declarations : a_generated_document
{
    const string Source = """
        using System;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        using FluentValidation;

        namespace Library.Authors.Registration;

        public record BareTypeId(string Value) : ConceptAs<string>(Value), IGeneratable<BareTypeId>
        {
            public static BareTypeId New() => new(GenerateValue.TypeId(""));
        }
        public record Base32Reference(string Value) : ConceptAs<string>(Value), IGeneratable<Base32Reference>
        {
            public static Base32Reference New() => new(GenerateValue.Bytes(16, ValueEncoding.Base32));
        }
        public record BinaryCode(string Value) : ConceptAs<string>(Value), IGeneratable<BinaryCode>
        {
            public static BinaryCode New() => new(GenerateValue.NanoId(64, "01"));
        }
        public record EncodedRandom(string Value) : ConceptAs<string>(Value), IGeneratable<EncodedRandom>
        {
            public static EncodedRandom New() => new(GenerateValue.Bytes(32));
        }
        public record HexReference(string Value) : ConceptAs<string>(Value), IGeneratable<HexReference>
        {
            public static HexReference New() => new(GenerateValue.Bytes(16, ValueEncoding.Hex));
        }
        public record SortableId(string Value) : ConceptAs<string>(Value), IGeneratable<SortableId>
        {
            public static SortableId New() => new(GenerateValue.Ulid());
        }
        public record StringUuid(string Value) : ConceptAs<string>(Value), IGeneratable<StringUuid>
        {
            public static StringUuid New() => new(Guid.NewGuid().ToString());
        }
        public record UserId(string Value) : ConceptAs<string>(Value), IGeneratable<UserId>
        {
            public static UserId New() => new(GenerateValue.TypeId("user"));
        }

        public record BookTitle(string Value) : ConceptAs<string>(Value);
        public class BookTitleValidator : ConceptValidator<BookTitle>
        {
            public BookTitleValidator() { RuleFor(value => value.Value).NotEmpty(); }
        }
        public record AuthorName(string Value) : ConceptAs<string>(Value);
        public enum PublicationState { Draft = 0, Published = 7 }
        public record Unrelated(string Value);

        [EventType]
        public record AuthorRegistered(AuthorName Name);
        """;

    void Because() => Generate((Analyzed.SlicePath, Source));

    [Theory]
    [InlineData("BareTypeId", "typeid", "generate typeid \"\"")]
    [InlineData("Base32Reference", "bytes", "generate bytes 16 base32")]
    [InlineData("BinaryCode", "nanoid", "generate nanoid 64 alphabet \"01\"")]
    [InlineData("EncodedRandom", "bytes", "generate bytes 32 base64url")]
    [InlineData("HexReference", "bytes", "generate bytes 16 hex")]
    [InlineData("SortableId", "ulid", "generate ulid")]
    [InlineData("StringUuid", "rule", "return Guid.NewGuid().ToString();")]
    [InlineData("UserId", "typeid", "generate typeid \"user\"")]
    public void should_recover_each_unused_generation_contract(string name, string kind, string declaration)
    {
        var concept = Result.Model.Concepts.Single(concept => concept.Name == name);
        concept.Primitive.ShouldEqual(ScreenplayPrimitive.String);
        concept.Generation!.Kind.ShouldEqual(kind);
        concept.HasGeneratedUses.ShouldBeFalse();
        Result.Source.ShouldContain($"concept {name}");
        Result.Source.ShouldContain(declaration);
    }

    [Fact] void should_recover_an_unused_concept_without_generation() => Result.Model.Concepts.Single(concept => concept.Name == "BookTitle").Generation.ShouldBeNull();
    [Fact] void should_attach_an_unused_concepts_validator() => Result.Model.Concepts.Single(concept => concept.Name == "BookTitle").Validations.Single().Kind.ShouldEqual(ValidationRuleKind.NotEmpty);
    [Fact] void should_not_duplicate_a_referenced_concept() => Result.Model.Concepts.Count(concept => concept.Name == "AuthorName").ShouldEqual(1);
    [Fact] void should_recover_an_unused_enumeration() => Result.Model.Concepts.Single(concept => concept.Name == "PublicationState").EnumValues.ShouldContainOnly(["Draft", "Published"]);
    [Fact] void should_not_treat_an_ordinary_record_as_a_concept() => Result.Model.Concepts.Any(concept => concept.Name == "Unrelated").ShouldBeFalse();
    [Fact] void should_preserve_every_declared_concept_through_a_valid_document() => AssertDocument();
    [Fact] void should_report_nothing() => Result.Diagnostics.ShouldBeEmpty();
}
