// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_consumed_inputs_change : given.a_signed_handler
{
    readonly Dictionary<string, AuthenticateResult> _results = [];

    async Task Because()
    {
        await Record("method", () => _context.Request.Method = "GET");
        await Record("path base", () => _context.Request.PathBase = "/other");
        await Record("path", () => _context.Request.Path = "/other");
        await Record("raw path", () => _context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/other");
        await Record("raw query", () => _context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/base/a%2fb?x=2");
        await Record("trust", () => _options.TrustForwardedIdentityHeaders = false);
        await Record("audience", () => _options.ForwardedIdentity.Audience = "other");
        await Record("key id", () => _options.ForwardedIdentity.Keys[0].KeyId = "K1");
        await Record("key secret", () => _options.ForwardedIdentity.Keys[0].SecretBase64 = Convert.ToBase64String(new byte[32]));
        await Record("tenancy configuration", () => _options.Tenancy.HttpHeader = "custom-tenant");
        await Record("inner enabled", () => _options.OnBehalfOf.Enabled = true);
        await Record("inner issuer", () => _options.OnBehalfOf.Issuer = "other");
        await Record("inner audience", () => _options.OnBehalfOf.Audience = "other");
        await Record("inner service", () => _options.OnBehalfOf.Service = "other");
        await Record("inner skew", () => _options.OnBehalfOf.ClockSkew = TimeSpan.FromSeconds(1));
        await Record("inner keys", () => _options.OnBehalfOf.SigningKeys.Add(new() { KeyId = "other", PublicKeyPem = "other" }));
        await Record("serializer/options reload", () => _options = new ArcOptions { TrustForwardedIdentityHeaders = true, ForwardedIdentity = _options.ForwardedIdentity });
        foreach (var name in new[] { MicrosoftIdentityPlatformHeaders.PrincipalHeader, MicrosoftIdentityPlatformHeaders.IdentityIdHeader, MicrosoftIdentityPlatformHeaders.IdentityNameHeader, ForwardedIdentityForSpecs.SignatureHeader, "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id", OnBehalfOfHeaders.ActorHeader, OnBehalfOfHeaders.AssertionHeader })
        {
            await Record(name, () => _context.Request.Headers[name] = "changed");
        }
    }

    [Fact] void should_not_reuse_the_complete_cache_after_any_consumed_input_changes() => _results.Where(pair => pair.Value.Succeeded).Select(pair => pair.Key).ShouldBeEmpty();

    async Task Record(string name, Action mutate)
    {
        ResetRequest();
        (await Authenticate()).Succeeded.ShouldBeTrue();
        mutate();
        _results[name] = await Authenticate();
    }
}
