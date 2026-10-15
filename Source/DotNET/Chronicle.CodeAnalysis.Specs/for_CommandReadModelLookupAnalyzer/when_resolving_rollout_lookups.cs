// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Independently readable source fragments are concatenated.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandReadModelLookupAnalyzer;

public class when_resolving_rollout_lookups
{
    [Fact]
    public Task should_not_report_an_explicit_lookup() => Verify(
        "[Command] public record Change(OwnerId Owner, MemberId From, MemberId To) { public Changed Handle([LookUpBy(nameof(From))] MemberState state) => new(); }");

    [Fact]
    public Task should_use_a_unique_matching_property_under_on() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner, MemberId Member) { public Changed Handle(MemberState state) => new(); }");

    [Fact]
    public Task should_report_primitive_key_ambiguity_under_on() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.On)] public record Change(OwnerId Owner, Guid From, Guid To) { public Changed Handle(RawState state) => new(); }",
        AnalyzerVerifier<CommandReadModelLookupAnalyzer>.Diagnostic("ARCCHR0023").WithSeverity(DiagnosticSeverity.Warning).WithArguments("From", "To", "LookUpBy"));

    [Fact]
    public Task should_explain_a_redirect_under_measure() => Verify(
        "[Command, ReadProtection(ReadProtectionMode.Measure)] public record Change(OwnerId Owner, MemberId Member) { public Changed Handle(MemberState state) => new(); }",
        AnalyzerVerifier<CommandReadModelLookupAnalyzer>.Diagnostic("ARCCHR0023").WithSeverity(DiagnosticSeverity.Info).WithArguments("Off and Measure", "Owner", "Member"));

    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) =>
        AnalyzerVerifier<CommandReadModelLookupAnalyzer>.VerifyAnalyzerAsync("using Cratis.Arc.Commands; " + DecisionAnalyzerSource.Definitions + source, diagnostics);
}
