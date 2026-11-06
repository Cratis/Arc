// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Validated signed credential metadata, not a claims projection or effective permissions.</summary>
/// <param name="CredentialKind">The signed credential category.</param>
/// <param name="CredentialId">The credential identifier.</param>
/// <param name="Issuer">The verified account issuer, if applicable.</param>
/// <param name="Subject">The verified account subject, if applicable.</param>
/// <param name="Idp">The identity provider, if applicable.</param>
/// <param name="AuthTime">The authentication time, if applicable.</param>
/// <param name="Acr">The assurance context, if applicable.</param>
/// <param name="Amr">The copied authentication methods.</param>
/// <param name="Actor">The current authenticated actor, if delegated.</param>
/// <param name="GrantId">The signed grant identifier.</param>
/// <param name="PolicyEpoch">The signed policy version.</param>
/// <param name="SenderBinding">The signed proof evidence.</param>
/// <param name="ForwardedTokenHash">The signed exact-token hash, if present.</param>
/// <param name="TenantTargets">The raw signed tenant targets, in canonical-then-alias order.</param>
/// <param name="TokenScope">The raw signed token scope.</param>
/// <param name="TokenClientId">The raw signed token client identifier.</param>
internal sealed record ForwardedIdentityV2Context(
    CallerCredentialKind CredentialKind,
    string CredentialId,
    string? Issuer,
    string? Subject,
    string? Idp,
    long? AuthTime,
    string? Acr,
    IReadOnlyList<string>? Amr,
    CallerActor? Actor,
    string? GrantId,
    ulong? PolicyEpoch,
    CallerProof SenderBinding,
    string? ForwardedTokenHash,
    IReadOnlyList<string> TenantTargets,
    string? TokenScope,
    string? TokenClientId)
{
    /// <summary>Validates metadata only after signature and principal verification.</summary>
    /// <param name="headers">The canonical signed fields.</param>
    /// <param name="realmIssuer">The configured realm issuer.</param>
    /// <param name="tenantHeader">The configured tenant header alias.</param>
    /// <returns>The validated context.</returns>
    internal static ForwardedIdentityV2Context Create(IReadOnlyDictionary<string, string> headers, string realmIssuer, string? tenantHeader = null)
    {
        try
        {
            var kind = ParseKind(Required(headers, "x-cratis-credential-kind"));
            var credentialId = Required(headers, "x-cratis-credential-id");
            var service = kind is CallerCredentialKind.MachinePrincipal or CallerCredentialKind.Workload;
            CheckPersonFields(headers, service, realmIssuer);
            CheckOptionalFields(headers);
            var methods = ReadMethods(headers);
            var actor = ReadActor(headers, kind);
            var binding = ReadBinding(Required(headers, "x-cratis-sender-binding"));
            var epoch = headers.GetValueOrDefault("x-cratis-policy-epoch");
            return new(kind, credentialId, headers.GetValueOrDefault("x-cratis-issuer"), headers.GetValueOrDefault("x-cratis-subject"), headers.GetValueOrDefault("x-cratis-idp"), service ? null : long.Parse(headers["x-cratis-auth-time"], CultureInfo.InvariantCulture), headers.GetValueOrDefault("x-cratis-acr"), methods, actor, headers.GetValueOrDefault("x-cratis-grant-id"), epoch is null ? null : ulong.Parse(epoch, CultureInfo.InvariantCulture), binding, headers.GetValueOrDefault("x-cratis-forwarded-token-sha256"), TenantTargetsFrom(headers, tenantHeader), headers.GetValueOrDefault("x-cratis-token-scope"), headers.GetValueOrDefault("x-cratis-token-client-id"));
        }
        catch (Exception error) when (error is FormatException or JsonException or DecoderFallbackException or InvalidOperationException)
        {
            throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        }
    }

    static ReadOnlyCollection<string> TenantTargetsFrom(IReadOnlyDictionary<string, string> headers, string? tenantHeader) =>
        Array.AsReadOnly(new[] { "x-cratis-tenant-id", "tenant-id", tenantHeader?.ToLowerInvariant() }
            .Where(name => name is not null)
            .Distinct(StringComparer.Ordinal)
            .Where(name => headers.ContainsKey(name!))
            .Select(name => headers[name!])
            .ToArray());

    static void CheckPersonFields(IReadOnlyDictionary<string, string> headers, bool service, string realmIssuer)
    {
        var personFields = new[] { "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-amr", "x-cratis-act" };
        if (service)
        {
            if (personFields.Any(headers.ContainsKey)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
            return;
        }
        if (Required(headers, "x-cratis-issuer") != realmIssuer) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        _ = Required(headers, "x-cratis-subject");
        _ = Required(headers, "x-cratis-idp");
        if (!NumericDate(Required(headers, "x-cratis-auth-time"))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        _ = Required(headers, "x-cratis-acr");
    }

    static void CheckOptionalFields(IReadOnlyDictionary<string, string> headers)
    {
        foreach (var name in new[] { "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id", "x-cratis-policy-epoch" })
        {
            if (headers.TryGetValue(name, out var value) && value.Length == 0) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        }
        if (headers.TryGetValue("x-cratis-policy-epoch", out var epoch) && !ForwardedIdentityV2Envelope.Unsigned(epoch, out _)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
    }

    static CallerCredentialKind ParseKind(string kind) => kind switch
    {
        "browser-session" => CallerCredentialKind.BrowserSession,
        "oauth-grant" => CallerCredentialKind.OAuthGrant,
        "pat" => CallerCredentialKind.PersonalAccessToken,
        "on-behalf-session" => CallerCredentialKind.OnBehalfSession,
        "machine-principal" => CallerCredentialKind.MachinePrincipal,
        "workload" => CallerCredentialKind.Workload,
        _ => throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata)
    };

    static ReadOnlyCollection<string>? ReadMethods(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("x-cratis-amr", out var methods)) return null;
        using var document = DecodeJson(methods);
        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.EnumerateArray().Any(method => method.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(method.GetString()))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        return Array.AsReadOnly(document.RootElement.EnumerateArray().Select(method => method.GetString()!).ToArray());
    }

    static CallerActor? ReadActor(IReadOnlyDictionary<string, string> headers, CallerCredentialKind kind)
    {
        if (kind == CallerCredentialKind.OnBehalfSession) _ = Required(headers, "x-cratis-act");
        if (!headers.TryGetValue("x-cratis-act", out var actorValue)) return null;
        using var document = DecodeJson(actorValue);
        CheckActor(document.RootElement, 0);
        _ = Required(headers, "x-cratis-grant-id");
        return new(document.RootElement.GetProperty("iss").GetString()!, document.RootElement.GetProperty("sub").GetString()!);
    }

    static CallerProof ReadBinding(string binding)
    {
        if (binding == "none") return new(CallerProofOutcome.NotPresented);
        using var document = DecodeJson(binding);
        var evidence = document.RootElement;
        if (evidence.ValueKind != JsonValueKind.Object || evidence.EnumerateObject().Count() != 2 || !StringField(evidence, "jkt") || !StringField(evidence, "jti")) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        try
        {
            _ = ForwardedIdentityV2Envelope.DecodeUrl(evidence.GetProperty("jkt").GetString()!, 32);
        }
        catch (Exception error) when (ForwardedIdentityV2Rejected.Is(error, out _))
        {
            throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        }
        return new(CallerProofOutcome.Verified, evidence.GetProperty("jkt").GetString(), evidence.GetProperty("jti").GetString());
    }

    static string Required(IReadOnlyDictionary<string, string> headers, string name) => headers.TryGetValue(name, out var value) && value.Length > 0 ? value : throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
    static JsonDocument DecodeJson(string value) => JsonDocument.Parse(new UTF8Encoding(false, true).GetString(Convert.FromBase64String(value)));
    static bool StringField(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());
    static bool NumericDate(string value) => long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var timestamp) && value == timestamp.ToString(CultureInfo.InvariantCulture);

    static void CheckActor(JsonElement actor, int depth)
    {
        if (depth > 8 || actor.ValueKind != JsonValueKind.Object || !StringField(actor, "iss") || !StringField(actor, "sub") || actor.EnumerateObject().Any(field => field.Name is not ("iss" or "sub" or "act"))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Metadata);
        if (actor.TryGetProperty("act", out var previous)) CheckActor(previous, depth + 1);
    }
}
