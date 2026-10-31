// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_consumed_request_bindings_change : given.a_signed_request
{
    readonly Dictionary<string, ForwardedIdentityAuthenticationResult> _results = [];

    void Because()
    {
        Record("method", () => _context.Method.Returns("GET"));
        Record("inner path", () => _context.Path.Returns("/other"));
        Record("raw path", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/other"));
        Record("raw query", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/base/a%2fb?x=2"));
        Record("id", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = ["other"]);
        Record("principal", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = ["other"]);
        Record("signature", () => _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = ["invalid"]);
        Record("ows", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = [" n "]);
        foreach (var name in new[] { "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id", OnBehalfOfHeaders.ActorHeader, OnBehalfOfHeaders.AssertionHeader })
        {
            Record(name, () => _rawHeaders[name] = ["changed"]);
        }
    }

    [Fact] void should_not_reuse_cached_authentication_for_changed_consumed_inputs() => _results.Where(pair => pair.Value.Principal is not null).Select(pair => pair.Key).ShouldBeEmpty();

    void Record(string name, Action mutate)
    {
        ResetRequest();
        Authenticate().Principal.ShouldNotBeNull();
        mutate();
        _results[name] = Authenticate();
    }
}
