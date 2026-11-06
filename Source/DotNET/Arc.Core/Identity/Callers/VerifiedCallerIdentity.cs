// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// The authenticated identity of a verified caller. Its claims are a fixed set derived from the caller's fields only,
/// and authority is always read from <see cref="Caller"/>, never from claims.
/// </summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal sealed class VerifiedCallerIdentity : ClaimsIdentity
{
    /// <summary>The authentication scheme and authentication type of a verified caller.</summary>
    internal const string AuthenticationScheme = "Cratis.VerifiedCaller";

    /// <summary>The claim carrying the checked tenant.</summary>
    internal const string TenantClaim = "cratis:tenant";

    /// <summary>The claim carrying the credential kind.</summary>
    internal const string CredentialKindClaim = "cratis:credential_kind";

    /// <summary>The claim carrying the credential identifier.</summary>
    internal const string CredentialIdClaim = "cratis:credential_id";

    /// <summary>The claim carrying each scope in the verified ceiling.</summary>
    internal const string ScopeClaim = "scope";

    /// <summary>The claim carrying the subject.</summary>
    internal const string SubjectClaim = "sub";

    /// <summary>The claim carrying an actor's issuer.</summary>
    internal const string IssuerClaim = "iss";

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifiedCallerIdentity"/> class.
    /// </summary>
    /// <param name="caller">The verified caller.</param>
    internal VerifiedCallerIdentity(VerifiedCaller caller)
        : base(BuildClaims(caller), AuthenticationScheme, SubjectClaim, ClaimTypes.Role)
    {
        Caller = caller;
        if (caller.Actor is { } actor)
        {
            Actor = new ClaimsIdentity([new Claim(SubjectClaim, actor.Subject), new Claim(IssuerClaim, actor.Issuer)]);
        }
    }

    VerifiedCallerIdentity(VerifiedCallerIdentity other)
        : base(other) => Caller = other.Caller;

    /// <summary>
    /// Gets the verified caller this identity represents.
    /// </summary>
    public VerifiedCaller Caller { get; }

    /// <inheritdoc/>
    public override ClaimsIdentity Clone() => new VerifiedCallerIdentity(this);

    /// <summary>
    /// Gets the subject of a verified caller: the person, or else the service principal.
    /// </summary>
    /// <param name="caller">The verified caller.</param>
    /// <returns>The subject, or null when the caller has neither.</returns>
    internal static string? SubjectOf(VerifiedCaller caller) => caller.Person?.Value ?? caller.ServicePrincipal?.Value;

    static List<Claim> BuildClaims(VerifiedCaller caller)
    {
        var claims = new List<Claim>();
        if (SubjectOf(caller) is { } subject)
        {
            claims.Add(new Claim(SubjectClaim, subject));
        }

        if (caller.Tenant is { } tenant)
        {
            claims.Add(new Claim(TenantClaim, tenant.Value));
        }

        claims.Add(new Claim(CredentialKindClaim, caller.CredentialKind.ToString()));
        claims.Add(new Claim(CredentialIdClaim, caller.CredentialId.Value));
        claims.AddRange(caller.Scopes.Order(StringComparer.Ordinal).Select(scope => new Claim(ScopeClaim, scope)));
        return claims;
    }
}
