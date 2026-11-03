// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Identity;

/// <summary>
/// Supplies public test-only vectors and an independent outbound producer for signed-forwarding specs.
/// </summary>
internal static class ForwardedIdentityForSpecs
{
    /// <summary>The wire signature header name.</summary>
    internal const string SignatureHeader = "x-cratis-forwarded-identity-signature";

    /// <summary>The exact public vector's base64 principal.</summary>
    internal const string Principal = "eyJpZGVudGl0eVByb3ZpZGVyIjoiYWFkIiwidXNlcklkIjoidSIsInVzZXJEZXRhaWxzIjoibiIsInVzZXJSb2xlcyI6W10sImNsYWltcyI6W119";

    /// <summary>The public vector's all-zero nonce, never a production nonce.</summary>
    internal const string Nonce = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    /// <summary>The externally computed vector MAC.</summary>
    internal const string Mac = "jgVyScoemstnZgj25AE70omcBbrUuzl9bIoesbV_BHo";

    /// <summary>The externally computed vector envelope.</summary>
    internal const string Envelope = $"v1.k1.1700000000.{Nonce}.{Mac}";

    /// <summary>The externally computed canonical input, including its final LF.</summary>
    internal const string Canonical = "Cratis.AuthProxy.ForwardedIdentity.v1\n8:arc-test\n2:k1\n10:1700000000\n43:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\n4:POST\n11:/base/a%2Fb\n1:3\n21:x-ms-client-principal\n112:" + Principal + "\n24:x-ms-client-principal-id\n1:u\n26:x-ms-client-principal-name\n1:n\n";

    /// <summary>Gets a fresh copy of the public test-only 00..1f key.</summary>
    internal static byte[] Key => Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();

    /// <summary>Gets the independently produced negative cases for the combined forwarding protocols.</summary>
    internal static IReadOnlyList<string> InvalidActorCases =>
    [
        "missing actor", "missing assertion", "malformed actor", "null actor claims", "disabled inner", "wrong inner key",
        "untrusted inner key", "expired inner", "future inner", "issuer", "audience", "service", "principal hash", "subject",
        "actor hash", "actor subject", "method", "path", "tenant"
    ];

    /// <summary>Creates explicitly enabled test-only verifier options.</summary>
    /// <returns>The options.</returns>
    internal static ArcOptions Options() => new()
    {
        TrustForwardedIdentityHeaders = true,
        ForwardedIdentity = new()
        {
            Enabled = true,
            Audience = "arc-test",
            Keys = [new() { KeyId = "k1", SecretBase64 = Convert.ToBase64String(Key) }]
        }
    };

    /// <summary>Creates the public vector's three identity headers.</summary>
    /// <returns>The unsigned headers.</returns>
    internal static Dictionary<string, string> Headers() => new(StringComparer.OrdinalIgnoreCase)
    {
        [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = Principal,
        [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = "u",
        [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "n"
    };

    /// <summary>Signs test headers independently, never calling Arc canonicalization or verification code.</summary>
    /// <param name="headers">The covered test headers, optionally including a previous signature.</param>
    /// <param name="method">The uppercase method.</param>
    /// <param name="path">The canonical escaped backend path.</param>
    /// <param name="timestamp">The Unix timestamp, or now.</param>
    /// <param name="nonce">The base64url nonce, or a new random nonce.</param>
    /// <param name="key">The test key, or the public vector key.</param>
    /// <param name="audience">The verifier audience.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>The v1 signature envelope.</returns>
    internal static string Sign(
        IReadOnlyDictionary<string, string> headers,
        string method = "POST",
        string path = "/base/a%2Fb",
        long? timestamp = null,
        string? nonce = null,
        byte[]? key = null,
        string audience = "arc-test",
        string keyId = "k1")
    {
        var ts = (timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString(CultureInfo.InvariantCulture);
        nonce ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var covered = headers.Where(pair => !pair.Key.Equals(SignatureHeader, StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key.ToLowerInvariant(), StringComparer.Ordinal).ToArray();
        var input = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v1\n");
        foreach (var value in new[] { audience, keyId, ts, nonce, method, path, covered.Length.ToString(CultureInfo.InvariantCulture) })
        {
            Append(input, value);
        }
        foreach (var pair in covered)
        {
            Append(input, pair.Key.ToLowerInvariant());
            Append(input, pair.Value.Trim(' ', '\t'));
        }
        using var hmac = new HMACSHA256(key ?? Key);
        var mac = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(input.ToString()))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"v1.{keyId}.{ts}.{nonce}.{mac}";
    }

    /// <summary>Adds an independently signed RS256 actor assertion to a test forward.</summary>
    /// <param name="headers">The represented identity headers to augment.</param>
    /// <param name="options">The test host options to enable for on-behalf forwarding.</param>
    /// <param name="now">The assertion issue time, or now.</param>
    /// <param name="expires">An optional assertion expiry, otherwise one minute after issue.</param>
    internal static void AddActor(IDictionary<string, string> headers, ArcOptions options, DateTimeOffset? now = null, DateTimeOffset? expires = null)
    {
        options.OnBehalfOf = OnBehalfOfAssertionsForSpecs.Options();
        var actor = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"identityProvider\":\"aad\",\"userId\":\"actor\",\"userDetails\":\"Actor\",\"userRoles\":[\"administrator\"],\"claims\":[]}"));
        headers[OnBehalfOfHeaders.ActorHeader] = actor;
        var claims = OnBehalfOfAssertionsForSpecs.ClaimsFor(headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader], actor, "u", "actor", now);
        claims["htm"] = "POST";
        claims["htu"] = "/base/a%2Fb";
        if (expires is not null)
        {
            claims["exp"] = expires.Value.ToUnixTimeSeconds();
        }

        headers[OnBehalfOfHeaders.AssertionHeader] = OnBehalfOfAssertionsForSpecs.Sign(claims);
    }

    /// <summary>Produces a broken inner forward to be covered by a fresh, valid outer signature.</summary>
    /// <param name="headers">The represented headers.</param>
    /// <param name="options">The test configuration.</param>
    /// <param name="scenario">The negative scenario.</param>
    /// <param name="now">The fixed verifier time.</param>
    /// <exception cref="ArgumentOutOfRangeException">The scenario is not one of <see cref="InvalidActorCases"/>.</exception>
    internal static void BreakActorForward(IDictionary<string, string> headers, ArcOptions options, string scenario, DateTimeOffset now)
    {
        AddActor(headers, options, now);
        var actor = headers[OnBehalfOfHeaders.ActorHeader];
        var claims = OnBehalfOfAssertionsForSpecs.ClaimsFor(headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader], actor, "u", "actor", now);
        claims["htm"] = "POST";
        claims["htu"] = "/base/a%2Fb";
        switch (scenario)
        {
            case "missing actor":
                headers.Remove(OnBehalfOfHeaders.ActorHeader);
                return;
            case "missing assertion":
                headers.Remove(OnBehalfOfHeaders.AssertionHeader);
                return;
            case "malformed actor":
                headers[OnBehalfOfHeaders.ActorHeader] = "not-base64";
                break;
            case "null actor claims":
                headers[OnBehalfOfHeaders.ActorHeader] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"userId\":\"actor\",\"claims\":null}"));
                break;
            case "disabled inner":
                options.OnBehalfOf.Enabled = false;
                break;
            case "wrong inner key":
                headers[OnBehalfOfHeaders.AssertionHeader] = OnBehalfOfAssertionsForSpecs.Sign(claims, keyId: "unknown");
                return;
            case "untrusted inner key":
                headers[OnBehalfOfHeaders.AssertionHeader] = OnBehalfOfAssertionsForSpecs.Sign(claims, key: OnBehalfOfAssertionsForSpecs.UnknownKey);
                return;
            case "expired inner":
                claims["exp"] = now.AddSeconds(-60).ToUnixTimeSeconds();
                break;
            case "future inner":
                claims["nbf"] = now.AddSeconds(60).ToUnixTimeSeconds();
                break;
            case "issuer":
                claims["iss"] = "other";
                break;
            case "audience":
                claims["aud"] = "other";
                break;
            case "service":
                options.OnBehalfOf.Service = "expected";
                claims["svc"] = "other";
                break;
            case "principal hash":
                claims["principal_hash"] = OnBehalfOfAssertionsForSpecs.Sha256("other");
                break;
            case "subject":
                claims["sub"] = "other";
                break;
            case "actor hash":
                claims["actor_hash"] = OnBehalfOfAssertionsForSpecs.Sha256("other");
                break;
            case "actor subject":
                claims["act"] = new Dictionary<string, object> { ["sub"] = "other", ["idp"] = "aad" };
                break;
            case "method":
                claims["htm"] = "GET";
                break;
            case "path":
                claims["htu"] = "/other";
                break;
            case "tenant":
                options.Tenancy.HttpHeader = "custom-tenant";
                headers["custom-tenant"] = "other";
                claims["tenant"] = "expected";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
        headers[OnBehalfOfHeaders.AssertionHeader] = OnBehalfOfAssertionsForSpecs.Sign(claims);
    }

    static void Append(StringBuilder builder, string value) => builder.Append(Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\n');
}
