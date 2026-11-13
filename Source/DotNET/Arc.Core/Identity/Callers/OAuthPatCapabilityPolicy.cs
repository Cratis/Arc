// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Server-owned capability policy and signed evidence, retained across cached caller resolution.</summary>
internal static class OAuthPatCapabilityPolicy
{
    static readonly object _required = new();
    static readonly object _context = new();

    /// <summary>Marks the capability's required credential shape without changing caller cache identity.</summary>
    /// <param name="items">Request-local state.</param>
    /// <param name="shape">The selected capability credential shape.</param>
    internal static void Require(IDictionary<object, object?> items, PresentedCredentialShape shape) => items[_required] = shape;

    /// <summary>Checks whether this capability requires signed coverage.</summary>
    /// <param name="items">Request-local state.</param>
    /// <returns>Whether an envelope is required.</returns>
    internal static bool IsEnvelopeRequired(IDictionary<object, object?> items) => items.TryGetValue(_required, out var value) && value is PresentedCredentialShape.Envelope;

    /// <summary>Checks whether composition admission applies to the current request.</summary>
    /// <param name="items">Request-local state.</param>
    /// <returns>Whether composition admission applies.</returns>
    internal static bool IsRequired(IDictionary<object, object?>? items) => items?.ContainsKey(_required) == true;

    /// <summary>Retains verified signed evidence for post-resolution admission.</summary>
    /// <param name="items">Request-local state.</param>
    /// <param name="context">Verified signed evidence.</param>
    internal static void Remember(IDictionary<object, object?> items, ForwardedIdentityV2Context context) => items[_context] = context;

    /// <summary>Checks direct unbound PAT/OAuth signed evidence before dispatch.</summary>
    /// <param name="context">Verified signed evidence.</param>
    /// <returns>Whether the evidence belongs to this composition.</returns>
    internal static bool AllowsContext(ForwardedIdentityV2Context context) =>
        IsBearerKind(context.CredentialKind) && HasDirectAccount(context) && HasNoSenderBinding(context);

    /// <summary>Checks the resolved caller, including previously cached callers.</summary>
    /// <param name="caller">The resolved caller.</param>
    /// <returns>Whether it is a direct unbound person bearer caller.</returns>
    internal static bool AllowsCaller(VerifiedCaller caller) =>
        caller.PrincipalKind == CallerPrincipalKind.Person && IsBearerKind(caller.CredentialKind) && HasNoDelegation(caller) && caller.SenderBinding is null;

    /// <summary>Rebinds retained signed context to a resolved caller.</summary>
    /// <param name="items">Request-local state.</param>
    /// <param name="caller">The resolved caller.</param>
    /// <returns>Whether both representations agree and are admissible.</returns>
    internal static bool AllowsEnvelope(IDictionary<object, object?> items, VerifiedCaller caller)
    {
        if (!items.TryGetValue(_context, out var value) || value is not ForwardedIdentityV2Context context) return false;
        return AllowsContext(context) && context.CredentialKind == caller.CredentialKind &&
            context.CredentialId == caller.CredentialId.Value && new CallerAccountKey(context.Issuer!, context.Subject!) == caller.AccountKey;
    }

    static bool HasNoDelegation(VerifiedCaller caller) => caller.Actor is null && caller.GrantId is null && caller.ServicePrincipal is null;

    static bool HasDirectAccount(ForwardedIdentityV2Context context) =>
        context.Actor is null && context.GrantId is null && !string.IsNullOrEmpty(context.Issuer) && !string.IsNullOrEmpty(context.Subject);

    static bool HasNoSenderBinding(ForwardedIdentityV2Context context) =>
        context.SenderBinding.Outcome == CallerProofOutcome.NotPresented && context.SenderBinding.KeyThumbprint is null && context.SenderBinding.Jti is null;

    static bool IsBearerKind(CallerCredentialKind kind) => kind is CallerCredentialKind.OAuthGrant or CallerCredentialKind.PersonalAccessToken;
}
