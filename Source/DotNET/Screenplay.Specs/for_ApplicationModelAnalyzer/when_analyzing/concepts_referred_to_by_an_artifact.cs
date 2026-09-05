// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A concept is declared once at the top of a document and referred to by name from there on, so every concept an
/// artifact refers to has to be collected while its type is resolved. The primitive behind each one has to survive
/// exactly - Screenplay tells a whole number from a fractional one, which the TypeScript proxy map does not.
/// </summary>
public class concepts_referred_to_by_an_artifact : Specification
{
    const string Source = """
        using System;
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : ConceptAs<Guid>(Value);

        [PII("The name of a person")]
        public record AuthorName(string Value) : ConceptAs<string>(Value);

        public record RoyaltyRate(decimal Value) : ConceptAs<decimal>(Value);

        public enum MembershipTier { Standard, Premium }

        [EventType]
        public record AuthorRegistered(AuthorId Id, AuthorName Name, RoyaltyRate Rate, MembershipTier Tier, int Books);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ConceptModel Concept(string name) => _analysis.Model.Concepts.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_declare_every_concept_referred_to() => _analysis.Model.Concepts.Select(_ => _.Name).ShouldContainOnly(["AuthorId", "AuthorName", "MembershipTier", "RoyaltyRate"]);
    [Fact] void should_order_them_by_name() => _analysis.Model.Concepts.First().Name.ShouldEqual("AuthorId");
    [Fact] void should_back_an_identity_with_a_uuid() => Concept("AuthorId").Primitive.ShouldEqual(ScreenplayPrimitive.Uuid);
    [Fact] void should_back_a_name_with_a_string() => Concept("AuthorName").Primitive.ShouldEqual(ScreenplayPrimitive.String);
    [Fact] void should_tell_a_fractional_number_from_a_whole_one() => Concept("RoyaltyRate").Primitive.ShouldEqual(ScreenplayPrimitive.Decimal);
    [Fact] void should_recover_that_a_concept_carries_personal_data() => Concept("AuthorName").IsPii.ShouldBeTrue();
    [Fact] void should_not_claim_the_others_do() => Concept("AuthorId").IsPii.ShouldBeFalse();
    [Fact] void should_declare_an_enumeration_referred_to() => Concept("MembershipTier").Primitive.ShouldEqual(ScreenplayPrimitive.Enum);
    [Fact] void should_recover_its_values() => Concept("MembershipTier").EnumValues.ShouldContainOnly(["Standard", "Premium"]);
    [Fact] void should_leave_a_primitive_undeclared() => _analysis.Model.Concepts.Any(_ => _.Name == "Int").ShouldBeFalse();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
