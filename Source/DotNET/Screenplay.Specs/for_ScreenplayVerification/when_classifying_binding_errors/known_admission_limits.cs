// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerification.when_classifying_binding_errors;

public class known_admission_limits : Specification
{
    [Theory]
    [InlineData("Command 'Register' handler requires a constrained implementation attachment.")]
    [InlineData("Concept 'AuthorName' compliance attributes require portable data-subject semantics.")]
    [InlineData("Read model 'Author' must have one unambiguous keyed query to identify instances in the first ESM v1 vertical.")]
    [InlineData("Query 'All' uses delivery, filtering, scope, or implementation behavior outside the first ESM v1 vertical.")]
    [InlineData("Query 'All' must declare one caller-supplied 'by' argument in the first ESM v1 vertical.")]
    [InlineData("Query 'All' must return one optional read model in the first ESM v1 vertical.")]
    void should_accept_the_known_fallback_in_both_modes(string message)
    {
        var verified = WithError("PLAY0268", message);
        verified.UnexpectedBindingErrors().ShouldBeEmpty();
        verified.UnexpectedBindingErrors(true).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Operations and systems are not admitted by any supported executable model (ESM) version yet (#301).")]
    [InlineData("Event sources, streams and routes are not admitted by any supported executable model (ESM) version yet (#302).")]
    void should_accept_additional_authoring_constructs_only_when_requested(string message)
    {
        var verified = WithError("PLAY0268", message);
        verified.UnexpectedBindingErrors().Count.ShouldEqual(1);
        verified.UnexpectedBindingErrors(true).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Command 'Register' reads 'Author' with legacy semantics that cannot imply decision consistency.")]
    [InlineData("Command 'Register' concurrency metadata keeps its legacy meaning and cannot bind to ESM v1.")]
    void should_preserve_documented_legacy_consistency_diagnostics(string message)
    {
        var verified = WithError("PLAY0271", message);
        verified.UnexpectedBindingErrors().ShouldBeEmpty();
        verified.UnexpectedBindingErrors(true).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Condition operand for 'role' must match its scalar type and declared enumeration values.")]
    [InlineData("Condition operand 'missing' must be a declared command property; read-model paths require decision-consistent reads (#129).")]
    [InlineData("Condition property 'other' must have the same scalar type as 'role'.")]
    [InlineData("Event reference 'Registered' is ambiguous across slices in the current ESM v1 binder.")]
    [InlineData("Read model reference 'Author' is ambiguous across slices in the current ESM v1 binder.")]
    [InlineData("Query reference 'ById' is ambiguous across slices in the current ESM v1 binder.")]
    [InlineData("Projection 'Author' declares a parent key outside a children block, where Chronicle never reads it.")]
    [InlineData("Query 'All' uses an unknown delivery shape.")]
    [InlineData("Specification event routes are not admitted by any supported executable model (ESM) version yet (#457).")]
    [InlineData("Unknown future admission message.")]
    void should_not_exempt_malformed_or_unknown_play0268_bindings(string message)
    {
        var verified = WithError("PLAY0268", message);
        verified.UnexpectedBindingErrors().Count.ShouldEqual(1);
        verified.UnexpectedBindingErrors(true).Count.ShouldEqual(1);
    }

    [Fact]
    void should_not_exempt_an_unknown_legacy_message() => WithError("PLAY0271", "An unknown legacy binding defect.").UnexpectedBindingErrors(true).Count.ShouldEqual(1);

    [Fact]
    void should_not_exempt_another_code_with_an_admission_message() => WithError("PLAY0273", "Query 'All' must return one optional read model in the first ESM v1 vertical.").UnexpectedBindingErrors(true).Count.ShouldEqual(1);

    [Fact]
    void should_keep_information_non_fatal()
    {
        var verified = new ScreenplayVerification(string.Empty, null, [])
        {
            BindingDiagnostics = [new(DiagnosticSeverity.Information, "PLAY0271", "Legacy consistency information.", SourceLocation.Start)]
        };
        verified.UnexpectedBindingErrors().ShouldBeEmpty();
    }

    static ScreenplayVerification WithError(string code, string message) => new(string.Empty, null, [])
    {
        BindingDiagnostics = [Diagnostic.Error(code, message, SourceLocation.Start)]
    };
}
