// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;

namespace Cratis.Arc.Identity;

/// <summary>
/// Reads the acting identity of an on-behalf forward from <see cref="OnBehalfOfHeaders.ActorHeader"/>.
/// </summary>
internal static class ForwardedActor
{
    /// <summary>
    /// The failure reason when an on-behalf forward arrives at a host that does not trust forwarded identity.
    /// </summary>
    internal const string UntrustedForward = "Not authenticated - on-behalf forwarding is only accepted from a trusted AuthProxy forward";

    /// <summary>
    /// The failure reason when the actor header arrives without the represented user.
    /// </summary>
    internal const string MissingRepresentedUser = "Not authenticated - on-behalf forward does not identify the represented user";

    /// <summary>
    /// The failure reason when the actor header does not hold a valid client principal.
    /// </summary>
    internal const string InvalidActor = "Not authenticated - invalid representation of the on-behalf actor";

    /// <summary>
    /// The failure reason when the actor and the represented user are the same identity.
    /// </summary>
    internal const string ActorIsRepresentedUser = "Not authenticated - an identity cannot act on behalf of itself";

    /// <summary>
    /// Checks whether the request carries the actor header.
    /// </summary>
    /// <param name="hasHeader">Checks whether the request has a header with the given name.</param>
    /// <returns>True if the actor header is present.</returns>
    internal static bool IsPresent(Func<string, bool> hasHeader) => hasHeader(OnBehalfOfHeaders.ActorHeader);

    /// <summary>
    /// Resolves the actor and attaches it to the represented user's identity as <see cref="ClaimsIdentity.Actor"/>.
    /// </summary>
    /// <param name="headerValue">The raw value of the actor header.</param>
    /// <param name="represented">The represented user's identity.</param>
    /// <param name="authenticationType">The authentication type for the actor identity.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to read the client principal with.</param>
    /// <returns>The failure reason, or <see langword="null"/> when the actor was attached.</returns>
    internal static string? Attach(string? headerValue, ClaimsIdentity represented, string authenticationType, JsonSerializerOptions serializerOptions)
    {
        var actor = Read(headerValue, serializerOptions);
        if (actor is null || string.IsNullOrWhiteSpace(actor.UserId))
        {
            return InvalidActor;
        }

        var representedId = represented.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.Equals(actor.UserId, representedId, StringComparison.Ordinal))
        {
            return ActorIsRepresentedUser;
        }

        represented.Actor = new ClaimsIdentity(ForwardedIdentityClaims.From(actor, actor.UserId), authenticationType);
        return null;
    }

    static ClientPrincipal? Read(string? headerValue, JsonSerializerOptions serializerOptions)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

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
