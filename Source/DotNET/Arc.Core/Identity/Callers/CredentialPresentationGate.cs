// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Refuses ambiguous or URI credentials before verification. Form-body credentials are not inspected: Arc commands and queries use JSON.
/// Raw header values are copied once; neither flattened headers nor request identity can replace this input.
/// Only the Bearer Authorization scheme is accepted in v1; the DPoP scheme is refused as MalformedAuthorization.
/// DPoP sender-binding transport support is deferred to the PAT/workload work.
/// A recognized session cookie counts as a credential: with an Authorization value or a non-v2 envelope it is refused as MultipleCredentials.
/// With a v2 envelope it is one credential, because the verified envelope is the session. Cookie values are matched by exact, case-sensitive name
/// across every Cookie header value and are never copied or logged. Hosts that flatten repeated Cookie headers cannot reveal split values; the
/// duplicate-preserving capability gate in <see cref="Inspect"/> already refuses those hosts.
/// </summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="reservedPrefixes">Optional additional retired namespaces. Built-in retired prefixes are always refused before dispatch.</param>
internal sealed class CredentialPresentationGate(ReservedCredentialPrefixes? reservedPrefixes = null)
{
    static readonly FrozenSet<string> _queryCredentials = new[]
    {
        "access_token", "accessToken", "id_token", "refresh_token", "token", "bearer", "pat",
        "session", "session_token", "credential", "authorization"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    static readonly FrozenSet<string> _sessionCookies = new[] { "cratis-identity-session", ".AspNetCore.Cookies" }.ToFrozenSet(StringComparer.Ordinal);

    readonly ReservedCredentialPrefixes _reservedPrefixes = reservedPrefixes ?? new([]);

    /// <summary>
    /// Isolated cardinality decision. A v2 envelope may carry a bearer for its verifier to bind.
    /// Mere co-presence is never proof of coverage; v1 envelopes remain separate credentials.
    /// </summary>
    /// <param name="presented">The copied credential set.</param>
    /// <returns>A presentation refusal, or null when exactly one credential is present.</returns>
    internal static CallerRefusal? DecideMultipleCredentials(CredentialPresentation presented)
    {
        if (HasRepeatedCredentialFields(presented))
        {
            return CallerRefusal.MultipleCredentials;
        }
        if (HasBearerAndEnvelope(presented) && !IsV2WithBearer(presented))
        {
            return CallerRefusal.MultipleCredentials;
        }
        if (presented.SessionCookiePresent && (HasBearerWithoutEnvelope(presented) || HasNonV2Envelope(presented)))
        {
            return CallerRefusal.MultipleCredentials;
        }
        return presented.Authorization.Count == 0 && presented.Envelope.Count == 0 ? CallerRefusal.NoCredential : null;
    }

    /// <summary>Copies raw credentials only when host cardinality can be trusted.</summary>
    /// <param name="request">The current HTTP request.</param>
    /// <returns>The copied set and any presentation refusal.</returns>
    internal CredentialPresentation Inspect(IHttpRequestContext request)
    {
        if (!TryReadRawRequest(request, out var raw))
        {
            return Refuse(CallerRefusal.UnreadableCredentialHeaders);
        }
        if (HasRetiredUriCredential(raw.RawTarget!))
        {
            return Refuse(CallerRefusal.LegacyCredentialUnsupported);
        }
        if (HasUriCredential(raw.RawTarget!))
        {
            return Refuse(CallerRefusal.CredentialInUri);
        }

        var authorization = Array.AsReadOnly(raw.GetHeaderValues("Authorization").ToArray());
        var envelope = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in raw.HeaderNames.Where(IsEnvelope).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            envelope.Add(name, Array.AsReadOnly(raw.GetHeaderValues(name).ToArray()));
        }
        if (authorization.Concat(envelope.Values.SelectMany(values => values)).Any(_reservedPrefixes.Refuses))
        {
            return Refuse(CallerRefusal.LegacyCredentialUnsupported);
        }
        var sessionCookie = raw.GetHeaderValues("Cookie").Any(HasSessionCookie);
        return Classify(new(authorization, envelope.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), sessionCookiePresent: sessionCookie));
    }

    static bool TryReadRawRequest(IHttpRequestContext request, out ICanReadForwardedIdentityRequest raw)
    {
        raw = (request as ICanReadForwardedIdentityRequest)!;
        return raw is not null && request is ICanPreserveCallerCredentialHeaders { PreservesDuplicateCredentialHeaders: true } && !string.IsNullOrEmpty(raw.RawTarget);
    }

    static bool HasRepeatedCredentialFields(CredentialPresentation presented) => presented.Authorization.Count > 1 || presented.Envelope.Values.Any(values => values.Count != 1);

    static bool HasSessionCookie(string? header) => header?.Split(';').Any(pair =>
    {
        var equals = pair.IndexOf('=');
        return (equals > 0 && _sessionCookies.Contains(pair[..equals].Trim())) || IsChunkedAspNetCookie(pair, equals);
    }) == true;

    static bool IsChunkedAspNetCookie(string pair, int equals)
    {
        const string prefix = ".AspNetCore.CookiesC";
        var name = equals > 0 ? pair[..equals].Trim() : string.Empty;
        return name.StartsWith(prefix, StringComparison.Ordinal) && name.Length > prefix.Length && name[prefix.Length..].All(char.IsAsciiDigit);
    }

    static bool HasBearerWithoutEnvelope(CredentialPresentation presented) => presented.Authorization.Count != 0 && presented.Envelope.Count == 0;

    static bool HasNonV2Envelope(CredentialPresentation presented) => presented.Envelope.Count != 0 &&
        !presented.Envelope.Values.All(values => values.Count == 1 && values[0].StartsWith("v2.", StringComparison.Ordinal));

    static bool HasBearerAndEnvelope(CredentialPresentation presented) => presented.Authorization.Count != 0 && presented.Envelope.Count != 0;

    static bool IsV2WithBearer(CredentialPresentation presented) => presented.Authorization.Count == 1 && presented.Envelope.Count == 1 &&
        presented.Envelope.Values.Single().Count == 1 && presented.Envelope.Values.Single()[0].StartsWith("v2.", StringComparison.Ordinal);

    static CredentialPresentation Classify(CredentialPresentation presented)
    {
        var refusal = DecideMultipleCredentials(presented);
        if (refusal is not null)
        {
            return new(presented.Authorization, presented.Envelope, refusal);
        }
        if (presented.Envelope.Count != 0)
        {
            return new(presented.Authorization, presented.Envelope, shape: PresentedCredentialShape.Envelope);
        }
        var token = ReadBearer(presented.Authorization[0]);
        if (token is null)
        {
            return new(presented.Authorization, presented.Envelope, CallerRefusal.MalformedAuthorization);
        }
        var shape = InPatNamespace(token) ? PresentedCredentialShape.PatBearer : PresentedCredentialShape.OAuthBearer;
        return new(presented.Authorization, presented.Envelope, shape: shape);
    }

    static CredentialPresentation Refuse(CallerRefusal reason) => new([], new Dictionary<string, IReadOnlyList<string>>(), reason);

    static bool IsEnvelope(string name) => name.Equals(ForwardedIdentityEnvelope.Header, StringComparison.OrdinalIgnoreCase);

    static string? ReadBearer(string value)
    {
        if (value?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) != true)
        {
            return null;
        }
        var token = value[7..];
        var padding = token.IndexOf('=');
        var content = padding < 0 ? token : token[..padding];
        if (content.Length == 0 || !content.All(IsBearerCharacter))
        {
            return null;
        }
        return padding >= 0 && !token[padding..].All(character => character == '=') ? null : token;
    }

    static bool IsBearerCharacter(char character) => character is
        (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '.' or '_' or '~' or '+' or '/';

    static bool InPatNamespace(string value)
    {
        var marker = value.IndexOf("_pat_", StringComparison.OrdinalIgnoreCase);
        return marker > 0 && value[..marker].All(character => character is
            (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-');
    }

    static bool HasUriCredential(string target)
    {
        var query = target.IndexOf('?');
        return query >= 0 && target[(query + 1)..].Split('&').Any(IsCredentialQueryParameter);
    }

    static bool IsCredentialQueryParameter(string pair)
    {
        var equals = pair.IndexOf('=');
        var key = equals < 0 ? pair : pair[..equals];
        var value = equals < 0 ? string.Empty : pair[(equals + 1)..];
        return _queryCredentials.Contains(Uri.UnescapeDataString(key.Replace('+', ' '))) || InPatNamespace(value);
    }

    bool HasRetiredUriCredential(string target)
    {
        var query = target.IndexOf('?');
        if (query < 0) return false;
        return target[(query + 1)..].Split('&').Any(pair =>
        {
            var equals = pair.IndexOf('=');
            return equals >= 0 && _reservedPrefixes.Refuses(Uri.UnescapeDataString(pair[(equals + 1)..].Replace('+', ' ')));
        });
    }
}
