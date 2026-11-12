// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.for_PatAdapterSurface;

public class when_inspecting_the_exported_types : Specification
{
    Type[] _exports;

    void Because() => _exports = typeof(IPersonalAccessTokens).Assembly.GetExportedTypes();

    [Fact]
    void should_export_only_the_trusted_contract() => _exports.Select(type => type.FullName).Order().ShouldEqual(new[]
    {
        "Cratis.Arc.PersonalAccessTokens.Contract.PatAdapterContract",
        "Cratis.Arc.PersonalAccessTokens.Contract.PatAdapterHarness",
        "Cratis.Arc.PersonalAccessTokens.Contract.PatAdapterInstance",
        "Cratis.Arc.PersonalAccessTokens.Contract.PatContractReport",
        "Cratis.Arc.PersonalAccessTokens.Contract.PatContractResult",
        "Cratis.Arc.PersonalAccessTokens.Development.DevelopmentPatArcBuilderExtensions",
        "Cratis.Arc.PersonalAccessTokens.IPatAuthority",
        "Cratis.Arc.PersonalAccessTokens.IPatPolicy",
        "Cratis.Arc.PersonalAccessTokens.IPatRecoveryEpoch",
        "Cratis.Arc.PersonalAccessTokens.IPersonalAccessTokens",
        "Cratis.Arc.PersonalAccessTokens.IssuedPat",
        "Cratis.Arc.PersonalAccessTokens.PatIssueResult",
        "Cratis.Arc.PersonalAccessTokens.PatIssuedIntent",
        "Cratis.Arc.PersonalAccessTokens.PatOptions",
        "Cratis.Arc.PersonalAccessTokens.PatOwner",
        "Cratis.Arc.PersonalAccessTokens.PatPolicyDecision",
        "Cratis.Arc.PersonalAccessTokens.PatRecord",
        "Cratis.Arc.PersonalAccessTokens.PatRevokeResult",
        "Cratis.Arc.PersonalAccessTokensArcBuilderExtensions"
    }.Order());

    [Fact]
    void should_keep_intent_owner_and_caller_construction_framework_owned() => new[] { typeof(PatIssuedIntent), typeof(PatOwner), typeof(VerifiedCaller) }
        .All(type => type.GetConstructors().Length == 0 && type.GetProperties().All(property => property.SetMethod?.IsPublic != true)).ShouldBeTrue();

    [Fact]
    void should_not_export_caller_minting_seams() => new[] { typeof(IPatIssuanceContext), typeof(CredentialVerification), typeof(IVerifiedCredentialSource), typeof(VerifiedCredentialFacts) }
        .Any(type => type.IsVisible).ShouldBeFalse();
}
