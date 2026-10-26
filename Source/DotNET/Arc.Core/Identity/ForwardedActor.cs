// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;

namespace Cratis.Arc.Identity;

/// <summary>
/// Verifies an on-behalf forward and attaches its actor to the represented user's identity.
/// </summary>
/// <remarks>
/// Shared by the ASP.NET Core scheme and the <c>Cratis.Arc.Core</c> authentication handler, so both apply the same rules.
/// </remarks>
internal static class ForwardedActor
{
    /// <summary>
    /// Checks whether the request carries the actor or the assertion header.
    /// </summary>
    /// <param name="hasHeader">Checks whether the request has a header with the given name.</param>
    /// <returns>True if either on-behalf header is present.</returns>
    internal static bool IsPresent(Func<string, bool> hasHeader) =>
        hasHeader(OnBehalfOfHeaders.ActorHeader) || hasHeader(OnBehalfOfHeaders.AssertionHeader);

    /// <summary>
    /// Decides whether the host accepts on-behalf forwards at all.
    /// </summary>
    /// <param name="options">The <see cref="ArcOptions"/>, or <see langword="null"/> when the host has none.</param>
    /// <returns>The failure reason, or <see langword="null"/> when on-behalf forwards may be verified.</returns>
    internal static string? Admit(ArcOptions? options)
    {
        if (options?.TrustForwardedIdentityHeaders != true)
        {
            return OnBehalfOfFailures.UntrustedForward;
        }

        return options.OnBehalfOf?.Enabled == true ? null : OnBehalfOfFailures.NotEnabled;
    }

    /// <summary>
    /// Verifies the forward and attaches the actor to the represented user's identity as <see cref="ClaimsIdentity.Actor"/>.
    /// </summary>
    /// <param name="forward">The <see cref="OnBehalfOfForward"/> values of the request.</param>
    /// <param name="represented">The represented user's <see cref="ClientPrincipal"/>.</param>
    /// <param name="identity">The represented user's identity.</param>
    /// <param name="authenticationType">The authentication type for the actor identity.</param>
    /// <param name="options">The <see cref="ArcOptions"/>.</param>
    /// <param name="time">The <see cref="TimeProvider"/> to check the assertion lifetime with.</param>
    /// <param name="replays">The <see cref="OnBehalfOfReplayCache"/> that refuses a reused assertion.</param>
    /// <returns>The failure reason, or <see langword="null"/> when the actor was attached.</returns>
    internal static string? Attach(
        OnBehalfOfForward forward,
        ClientPrincipal represented,
        ClaimsIdentity identity,
        string authenticationType,
        ArcOptions options,
        TimeProvider time,
        OnBehalfOfReplayCache replays)
    {
        var admission = Admit(options);
        if (admission is not null)
        {
            return admission;
        }

        if (IsRepeated(forward.Actor) || IsRepeated(forward.Assertion))
        {
            return OnBehalfOfFailures.RepeatedHeader;
        }

        if (string.IsNullOrWhiteSpace(forward.Actor))
        {
            return OnBehalfOfFailures.MissingActor;
        }

        if (string.IsNullOrWhiteSpace(forward.Assertion))
        {
            return OnBehalfOfFailures.MissingAssertion;
        }

        var keys = OnBehalfOfSigningKeys.For(options.OnBehalfOf);
        if (keys.Problem is not null)
        {
            return $"{OnBehalfOfFailures.Misconfigured}: {keys.Problem}";
        }

        var now = time.GetUtcNow();
        var failure = OnBehalfOfAssertionVerifier.Verify(forward.Assertion, keys, options.OnBehalfOf, now, out var assertion) ??
            OnBehalfOfForwardBinding.Check(assertion!, forward, represented);
        if (failure is not null)
        {
            return failure;
        }

        var actor = Read(forward.Actor, options.JsonSerializerOptions);
        if (actor is null || string.IsNullOrWhiteSpace(actor.UserId))
        {
            return OnBehalfOfFailures.InvalidActor;
        }

        if (!string.Equals(actor.UserId, assertion!.ActorSubject, StringComparison.Ordinal))
        {
            return OnBehalfOfFailures.ActorMismatch;
        }

        if (string.Equals(actor.UserId, identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, StringComparison.Ordinal))
        {
            return OnBehalfOfFailures.ActorIsRepresentedUser;
        }

        if (!replays.TryRecord(assertion.TokenId, assertion.Expires + options.OnBehalfOf.ClockSkew, now))
        {
            return OnBehalfOfFailures.Replayed;
        }

        identity.Actor = new ClaimsIdentity(ForwardedIdentityClaims.From(actor, actor.UserId), authenticationType);
        return null;
    }

    /// <summary>
    /// Checks whether a header was repeated. A repeated header arrives joined by a comma, which neither base64 nor a
    /// compact JWS contains.
    /// </summary>
    /// <param name="value">The header value.</param>
    /// <returns>True when the header was repeated.</returns>
    static bool IsRepeated(string? value) => value?.Contains(',') == true;

    static ClientPrincipal? Read(string headerValue, JsonSerializerOptions serializerOptions)
    {
        try
        {
            return JsonSerializer.Deserialize(Convert.FromBase64String(headerValue), serializerOptions.ResolveTypeInfo<ClientPrincipal>());
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            // A malformed actor is a rejected forward; the caller reports the failure.
            return null;
        }
    }
}
