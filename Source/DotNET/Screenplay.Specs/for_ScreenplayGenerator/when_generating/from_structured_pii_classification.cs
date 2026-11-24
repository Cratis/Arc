// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_structured_pii_classification : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Arc.Chronicle.Commands;
        using Cratis.Chronicle.ProtectedValues;
        using Cratis.Concepts;

        namespace Cratis.Chronicle.Compliance.GDPR
        {
            public enum SpecialCategory
            {
                None = 0, RacialOrEthnicOrigin = 91, PoliticalOpinions = 12,
                ReligiousOrPhilosophicalBeliefs = 83, TradeUnionMembership = 24,
                Genetic = 75, Biometric = 36, Health = 67, SexLifeOrSexualOrientation = 48
            }
            public class PIIAttribute : System.Attribute
            {
                public SpecialCategory SpecialCategory { get; set; }
                public bool Criminal { get; set; }
                public bool OperationalSecret { get; set; }
                public string Details { get; set; }
            }
        }

        namespace Library.Authors.Registration
        {
            [PII(SpecialCategory = SpecialCategory.RacialOrEthnicOrigin)]
            public record Ethnicity(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.PoliticalOpinions)]
            public record Politics(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.ReligiousOrPhilosophicalBeliefs)]
            public record Beliefs(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.TradeUnionMembership)]
            public record Union(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.Genetic)]
            public record Genetics(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.Biometric)]
            public record Biometrics(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = (SpecialCategory)67)]
            public record Medical(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.SexLifeOrSexualOrientation)]
            public record Sexuality(string Value) : ConceptAs<string>(Value);
            [PII(Criminal = true)]
            public record Conviction(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.Health, Criminal = true)]
            public record MedicalConviction(string Value) : ConceptAs<string>(Value);
            [PII(OperationalSecret = true), NotAudited]
            public record PersonalSecret(string Value) : ConceptAs<string>(Value);
            [PII(OperationalSecret = true)]
            public record PartialPersonalSecret(string Value) : ConceptAs<string>(Value);
            [PII, Encrypted, NotAudited]
            public record LegacyPersonalSecret(string Value) : ConceptAs<string>(Value);
            [PII(Details = "special health criminal secret")]
            public record PlainPersonal(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = SpecialCategory.None, Criminal = false, OperationalSecret = false)]
            public record DefaultPersonal(string Value) : ConceptAs<string>(Value);
            [PII(SpecialCategory = (SpecialCategory)999)]
            public record UnknownPersonal(string Value) : ConceptAs<string>(Value);
            [Encrypted(EncryptionScope.Namespace), NotAudited]
            public record ScopedSecret(string Value) : ConceptAs<string>(Value);
        }
        """));

    [Fact] void should_read_racial_or_ethnic_origin() => AssertCategory("Ethnicity", "racialOrEthnicOrigin");
    [Fact] void should_read_political_opinions() => AssertCategory("Politics", "politicalOpinions");
    [Fact] void should_read_religious_or_philosophical_beliefs() => AssertCategory("Beliefs", "religiousOrPhilosophicalBeliefs");
    [Fact] void should_read_trade_union_membership() => AssertCategory("Union", "tradeUnionMembership");
    [Fact] void should_read_genetic_data() => AssertCategory("Genetics", "genetic");
    [Fact] void should_read_biometric_data() => AssertCategory("Biometrics", "biometric");
    [Fact] void should_resolve_health_by_the_enum_constant() => AssertCategory("Medical", "health");
    [Fact] void should_read_sex_life_or_sexual_orientation() => AssertCategory("Sexuality", "sexLifeOrSexualOrientation");
    [Fact] void should_read_criminal_data() => Declaration("Conviction").Attributes.Single().Criminal.ShouldBeTrue();
    [Fact] void should_read_both_personal_qualifiers()
    {
        AssertCategory("MedicalConviction", "health");
        Declaration("MedicalConviction").Attributes.Single().Criminal.ShouldBeTrue();
    }
    [Fact] void should_read_the_complete_personal_secret() => Result.Source.ShouldContain("concept PersonalSecret : String pii secret");
    [Fact] void should_keep_the_legacy_personal_secret() => Result.Source.ShouldContain("concept LegacyPersonalSecret : String pii secret");
    [Fact] void should_report_only_the_incomplete_secret() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.PartialSecretMarking).Message.ShouldContain("PartialPersonalSecret");
    [Fact] void should_warn_about_the_legacy_encryption() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0087").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_explain_the_legacy_replacement() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0087").Message.ShouldContain("PIIAttribute.OperationalSecret");
    [Fact] void should_warn_about_the_unknown_enum_value() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0088").Message.ShouldContain("UnknownPersonal");
    [Fact] void should_not_guess_a_category_for_an_unknown_value() => Declaration("UnknownPersonal").Attributes.Single().SpecialCategory.ShouldBeNull();
    [Fact] void should_not_infer_qualifiers_from_details()
    {
        Declaration("PlainPersonal").Attributes.Single().SpecialCategory.ShouldBeNull();
        Declaration("PlainPersonal").Attributes.Single().Criminal.ShouldBeFalse();
        Declaration("PlainPersonal").Attributes.Count().ShouldEqual(1);
    }
    [Fact] void should_keep_plain_personal_data() => Result.Source.ShouldContain("concept PlainPersonal : String pii");
    [Fact] void should_leave_the_default_category_unqualified() => Declaration("DefaultPersonal").Attributes.Single().SpecialCategory.ShouldBeNull();
    [Fact] void should_keep_secret_only_scope_recovery() => Result.Model.Concepts.Single(concept => concept.Name == "ScopedSecret").EncryptionScope.ShouldEqual("namespace");
    [Fact] void should_compile_and_round_trip() => AssertDocument();

    Cratis.Screenplay.Syntax.ConceptSyntax Declaration(string name) => new Cratis.Screenplay.ScreenplayCompiler().Compile(Result.Source).Value!.Concepts.Single(concept => concept.Name == name);

    void AssertCategory(string name, string category) => Declaration(name).Attributes.Single().SpecialCategory.ShouldEqual(category);
}
