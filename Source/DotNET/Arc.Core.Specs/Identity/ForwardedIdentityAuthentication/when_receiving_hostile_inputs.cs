// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_receiving_hostile_inputs : given.a_signed_request
{
    readonly Dictionary<string, ForwardedIdentityAuthenticationResult> _results = [];

    void Because()
    {
        Record("missing signature", () => _rawHeaders.Remove(ForwardedIdentityForSpecs.SignatureHeader));
        Record("duplicate signature", () => _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope, ForwardedIdentityForSpecs.Envelope]);
        Record("duplicate field", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = ["n", "n"]);
        Record("missing id", () => _rawHeaders.Remove(MicrosoftIdentityPlatformHeaders.IdentityIdHeader));
        Record("empty name", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = [string.Empty]);
        Record("forged name", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = ["forged"]);
        Record("forged principal", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = ["AAAA"]);
        Record("forged token", () => _rawHeaders["x-cratis-token-scope"] = ["admin"]);
        Record("forged tenant", () => _rawHeaders["tenant-id"] = ["other"]);
        Record("forged service", () => _rawHeaders["service-id"] = ["other"]);
        Record("unknown identity suffix", () => _rawHeaders["X-MS-CLIENT-PRINCIPAL-FUTURE"] = ["new"]);
        Record("unknown actor suffix", () => _rawHeaders["x-cratis-on-behalf-future"] = ["new"]);
        Record("wrong method", () => _context.Method.Returns("GET"));
        Record("decoded slash", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/base/a/b"));
        Record("double encoded slash", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/base/a%252Fb"));
        Record("absolute target", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("https://host/base/a%2Fb"));
        Record("malformed escape", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/base/a%2"));
        Record("fragment", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/base/a%2Fb#fragment"));
        Record("no raw target", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns((string?)null));
        Record("control value", () => _rawHeaders["tenant-id"] = ["a\rb"]);
        Record("non-ascii value", () => _rawHeaders["tenant-id"] = ["é"]);
        Record("header bound", () => { for (var index = 0; index < 65; index++) { _rawHeaders[$"x-ms-client-principal-{index}"] = ["v"]; } });
        Record("byte bound", () => _rawHeaders["tenant-id"] = [new string('x', 32768)]);
        Record("unknown key", () => _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope.Replace(".k1.", ".unknown.")]);
        Record("unknown version", () => _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope.Replace("v1.", "v2.")]);
        Record("extra field", () => _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope + ".extra"]);
        Record("padded mac", () => _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope + "="]);
        Record("leading zero timestamp", () => _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Envelope.Replace(".1700000000.", ".01700000000.")]);
        Record("expired", () => Resign(1699999939));
        Record("future", () => Resign(1700000031));
        Record("subject mismatch", () => _headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = "other", true);
        Record("bad extended id", () => _headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = "UTF-8''%FF", true);
        Record("malformed json", () => _headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{")), true);
        Record("invalid utf8", () => _headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = Convert.ToBase64String([0xff]), true);
        Record("null claims", () => SetPrincipal("{\"userId\":\"u\",\"claims\":null,\"userRoles\":[]}"), true);
        Record("null roles", () => SetPrincipal("{\"userId\":\"u\",\"claims\":[],\"userRoles\":null}"), true);
        Record("null claim", () => SetPrincipal("{\"userId\":\"u\",\"claims\":[null],\"userRoles\":[]}"), true);
        Record("missing subject", () => SetPrincipal("{\"claims\":[],\"userRoles\":[]}"), true);
        Record("orphan actor", () => _headers[OnBehalfOfHeaders.ActorHeader] = ForwardedIdentityForSpecs.Principal, true);
        Record("orphan assertion", () => _headers[OnBehalfOfHeaders.AssertionHeader] = "invalid", true);
    }

    [Fact] void should_never_promote_a_hostile_identity() => _results.Where(pair => pair.Value.Principal is not null).Select(pair => pair.Key).ShouldBeEmpty();
    [Fact] void should_fail_with_value_free_reason_codes() => _results.Values.All(result => result.Failure == "forwarded-identity-invalid-input" || result.Failure == "forwarded-identity-rejected" || result.Failure == "forwarded-identity-invalid-actor").ShouldBeTrue();

    void SetPrincipal(string json) => _headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    void Record(string name, Action mutate, bool sign = false)
    {
        ResetRequest();
        mutate();
        if (sign)
        {
            Resign();
        }
        _results[name] = Authenticate();
    }
}
