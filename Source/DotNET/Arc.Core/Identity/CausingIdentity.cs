// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Identity;

/// <summary>
/// Describes the caller of a command or query, as Arc sees the operation.
/// </summary>
/// <remarks>
/// This is Arc's view of the operation's caller. It is <b>not</b> the Chronicle audit identity: for the same
/// anonymous operation Chronicle may record <c>System</c> as the cause, so never use this value as the audit record.
/// <para>
/// <see cref="IsAnonymous"/> and <see cref="IsSystem"/> are not constructor parameters. Only <see cref="Anonymous"/>
/// and <see cref="System"/> carry them, so a value built from forged claims, even one with the subject
/// <c>[System]</c>, is neither anonymous nor system and never equals those instances.
/// </para>
/// </remarks>
/// <param name="Subject">The subject identifying the caller.</param>
/// <param name="Name">The display name of the caller.</param>
/// <param name="UserName">The user name of the caller.</param>
/// <param name="OnBehalfOf">The identity represented by the caller, when the caller acts on behalf of someone.</param>
public sealed record CausingIdentity(IdentityId Subject, IdentityName Name, IdentityUserName UserName, CausingIdentity? OnBehalfOf = null)
{
    /// <summary>
    /// Represents an unauthenticated or unidentified caller.
    /// </summary>
    public static readonly CausingIdentity Anonymous = new("[Anonymous]", "[Anonymous]", "[Anonymous]") { IsAnonymous = true };

    /// <summary>
    /// Represents the trusted system actor.
    /// </summary>
    public static readonly CausingIdentity System = new(SystemPrincipal.Subject, SystemPrincipal.Subject, SystemPrincipal.Subject) { IsSystem = true };

    /// <summary>
    /// Gets a value indicating whether the caller is anonymous.
    /// </summary>
    public bool IsAnonymous { get; internal init; }

    /// <summary>
    /// Gets a value indicating whether the caller is the trusted system actor.
    /// </summary>
    public bool IsSystem { get; internal init; }

    /// <summary>
    /// Maps a principal to the identity that caused an operation.
    /// </summary>
    /// <param name="principal">The principal to map.</param>
    /// <returns>The identity, <see cref="Anonymous"/> when the principal is missing, unauthenticated or carries no identifying claims.</returns>
    internal static CausingIdentity From(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        if (principal.Identity.AuthenticationType == SystemPrincipal.AuthenticationType &&
            principal.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value == SystemPrincipal.Subject)
        {
            return System;
        }

        var represented = FromClaims(principal);
        if (principal.GetActor() is not { } actor)
        {
            return represented ?? Anonymous;
        }

        var acting = FromClaims(new ClaimsPrincipal(actor));
        if (acting is null)
        {
            return represented ?? Anonymous;
        }

        return represented is null ? acting : acting with { OnBehalfOf = represented };
    }

    static CausingIdentity? FromClaims(ClaimsPrincipal principal)
    {
        var subject = principal.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value ?? string.Empty;
        var name = principal.Claims.FirstOrDefault(_ => _.Type == "name")?.Value ?? principal.Identity?.Name ?? string.Empty;
        var userName = principal.Claims.FirstOrDefault(_ => _.Type == "preferred_username")?.Value ?? principal.Identity?.Name ?? string.Empty;
        return string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(userName)
            ? null
            : new CausingIdentity(subject, name, userName);
    }
}
