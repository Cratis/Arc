// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_metadata;

/// <summary>
/// Recovers the generation strategies and validations rendered for the GenerationSupport V21 concepts.
/// </summary>
/// <remarks>
/// The input follows the Stage renderer's concept shapes (SemanticCommonArtifactRenderer, SemanticGenerationRendering and
/// SemanticValidationRendering); no rendered V21 output exists in the Stage analysis evidence, so this is authored from
/// the renderer source, not from a fresh Stage render.
/// </remarks>
public class from_generation_support_v21 : a_generated_document
{
    const string Source = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        using FluentValidation;
        namespace Library.Common;

        public record OrderId(Guid Value) : ConceptAs<Guid>(Value);

        public record InvoiceRef(string Value) : ConceptAs<string>(Value), IGeneratable<InvoiceRef>
        {
            public static InvoiceRef New() => new(GenerateValue.Pattern("[AB]", Checksum.Luhn));
            public static implicit operator string(InvoiceRef value) => value.Value;
            public static implicit operator InvoiceRef(string value) => new(value);
        }

        public class InvoiceRefValidator : Cratis.Arc.Validation.ConceptValidator<InvoiceRef>
        {
            public InvoiceRefValidator()
            {
                RuleFor(_ => _.Value).Must(value => value is null || GenerateValue.HasValidChecksum(value, Checksum.Luhn));
            }
        }

        public record TicketNumber(long Value) : ConceptAs<long>(Value), IGeneratable<TicketNumber>
        {
            public static TicketNumber New() => new(GenerateValue.Integer(-9223372036854775808L, 9223372036854775807L));
            public static implicit operator long(TicketNumber value) => value.Value;
            public static implicit operator TicketNumber(long value) => new(value);
        }

        public record TenantOrderId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<TenantOrderId>
        {
            public static TenantOrderId New(GenerationContext context) => new(TenantOrder(context));
            static string TenantOrder(GenerationContext context)
            {
                return context.Tenant.ToString();
            }
            public static implicit operator string(TenantOrderId value) => value.Value;
            public static implicit operator TenantOrderId(string value) => new(value);
        }

        [EventType] public record InvoiceRegistered(InvoiceRef Invoice, TicketNumber Ticket, TenantOrderId Order);

        [Command] public record RegisterInvoice([Cratis.Chronicle.Keys.Key] OrderId Id)
        {
            public InvoiceRegistered Handle([GeneratedValue("invoice")] InvoiceRef invoice, [GeneratedValue("ticket")] TicketNumber ticket, [GeneratedValue("order")] TenantOrderId order) => new(invoice, ticket, order);
        }
        """;

    static readonly string[] _concepts = ["InvoiceRef", "OrderId", "TenantOrderId", "TicketNumber"];

    void Establish() => Generate((Analyzed.SlicePath, Source));

    ConceptModel Concept(string name) => Result.Model.Concepts.Single(concept => concept.Name == name);

    [Fact]
    void should_have_a_source_that_compiles() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();

    [Fact]
    void should_recover_exactly_the_declared_concepts() =>
        Assert.Equal(_concepts, Result.Model.Concepts.Select(concept => concept.Name).Order());

    [Fact]
    void should_recover_the_invoice_ref_pattern_with_its_checksum()
    {
        Result.Source.ShouldContain("generate pattern \"[AB]\" checksum luhn");
        Concept("InvoiceRef").Generation.ShouldNotBeNull();
    }

    [Fact]
    void should_recover_the_invoice_ref_checksum_validation()
    {
        var rule = Assert.Single(Concept("InvoiceRef").Validations);
        rule.Kind.ShouldEqual(ValidationRuleKind.Checksum);
        rule.Value.ShouldEqual("luhn");
        Result.Source.ShouldContain("validate");
    }

    [Fact]
    void should_recover_the_ticket_number_bounds_without_narrowing() =>
        Result.Source.ShouldContain("generate random -9223372036854775808..9223372036854775807");

    [Fact]
    void should_recover_the_tenant_order_rule_body()
    {
        Result.Source.ShouldContain("generate rule TenantOrderIdNew");
        Result.Source.ShouldContain("return context.Tenant.ToString();");
    }

    [Fact]
    void should_report_no_unrecoverable_generation_or_validation() =>
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable || diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeEmpty();

    [Fact]
    void should_keep_the_document_compiling_binding_and_stable()
    {
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    }
}
