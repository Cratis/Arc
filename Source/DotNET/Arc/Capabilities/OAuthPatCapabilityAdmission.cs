// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities;

/// <summary>Checks installed composition policy independently of caller-resolution caching.</summary>
internal static class OAuthPatCapabilityAdmission
{
    /// <summary>Checks whether OAuth/PAT composition was installed for this resource.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>Whether the composition is installed.</returns>
    internal static bool IsInstalled(HttpContext context) => context.RequestServices.GetService<OAuthCapableCallerInstallation>() is not null;

    /// <summary>Retains capability-specific policy independently of caller cache identity.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="presentation">The selected credential presentation.</param>
    internal static void Prepare(HttpContext context, CredentialPresentation presentation)
    {
        if (IsInstalled(context)) OAuthPatCapabilityPolicy.Require(context.Items, presentation.Shape!.Value);
    }

    /// <summary>Applies composition admission to newly resolved and cached callers.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="caller">The resolved caller.</param>
    /// <returns>Whether the caller satisfies the endpoint's composition policy.</returns>
    internal static bool Allows(HttpContext context, VerifiedCaller caller)
    {
        if (!IsInstalled(context)) return true;
        if (!OAuthPatCapabilityPolicy.AllowsCaller(caller)) return false;
        if (caller.CredentialKind == CallerCredentialKind.OAuthGrant && context.GetEndpoint()?.Metadata.GetMetadata<AcceptsOAuthCapability>() is null) return false;
        if (OAuthPatCapabilityPolicy.IsEnvelopeRequired(context.Items))
        {
            return OAuthPatCapabilityPolicy.AllowsEnvelope(context.Items, caller);
        }
        return caller.CredentialKind == CallerCredentialKind.PersonalAccessToken;
    }
}
