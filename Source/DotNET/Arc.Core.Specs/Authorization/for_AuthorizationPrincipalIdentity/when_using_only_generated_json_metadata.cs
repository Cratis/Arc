// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity;

public class when_using_only_generated_json_metadata : Specification
{
    ClaimsPrincipal _principal;
    JsonSerializerOptions _options;
    string _contextOnly;
    string _fingerprint;

    void Establish()
    {
        var identity = new ClaimsIdentity([new Claim("subject", "æøå<&>😀")], "test");
        _principal = new ClaimsPrincipal(identity);
        _options = new JsonSerializerOptions { TypeInfoResolver = PrincipalIdentityJsonSerializerContext.Default };
        IEnumerable<PrincipalIdentityContent> content =
        [
            new(
                typeof(ClaimsIdentity).FullName,
                identity.AuthenticationType,
                identity.NameClaimType,
                identity.RoleClaimType,
                identity.Label,
                new PrincipalBootstrapContent(false, null),
                null,
                [new PrincipalClaimContent("subject", "æøå<&>😀", ClaimValueTypes.String, ClaimsIdentity.DefaultIssuer, ClaimsIdentity.DefaultIssuer, [])])
        ];
        _contextOnly = JsonSerializer.Serialize(content, _options);
    }

    void Because() => _fingerprint = AuthorizationPrincipalIdentity.Capture(_principal).Fingerprint;

    [Fact] void should_match_serialization_with_only_the_generated_resolver() => _fingerprint.ShouldEqual(_contextOnly);
    [Fact] void should_not_have_a_reflection_resolver_fallback() => _options.TypeInfoResolver.ShouldBeSame(PrincipalIdentityJsonSerializerContext.Default);
    [Fact] void should_not_supply_metadata_for_arbitrary_reflected_types() => PrincipalIdentityJsonSerializerContext.Default.GetTypeInfo(typeof(ClaimsPrincipal)).ShouldBeNull();
}
