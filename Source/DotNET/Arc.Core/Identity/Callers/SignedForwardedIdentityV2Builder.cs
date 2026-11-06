// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Identity.ForwardedIdentityV2;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Configures public verification keys and explicit replay protection for a v2-only browser hop.
/// </summary>
public sealed class SignedForwardedIdentityV2Builder
{
    readonly Dictionary<string, ForwardedIdentityV2PublicKey> _keys = new(StringComparer.Ordinal);
    string? _realmIssuer;
    string? _tenantHeader;
    ServiceDescriptor? _nonceStore;

    /// <summary>
    /// Sets the exact trusted account issuer.
    /// </summary>
    /// <param name="realmIssuer">The trusted issuer.</param>
    /// <returns>This builder.</returns>
    public SignedForwardedIdentityV2Builder WithRealmIssuer(string realmIssuer)
    {
        _realmIssuer = realmIssuer;
        return this;
    }

    /// <summary>
    /// Includes a custom tenant header in signature coverage.
    /// </summary>
    /// <param name="headerName">The raw tenant header name.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException">The header name is invalid.</exception>
    public SignedForwardedIdentityV2Builder WithTenantHeader(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName) || headerName.Any(c => !char.IsAsciiLetterOrDigit(c) && !"!#$%&'*+-.^_`|~".Contains(c)))
        {
            throw new ArgumentException("A tenant header must be an HTTP field name.", nameof(headerName));
        }
        _tenantHeader = headerName;
        return this;
    }

    /// <summary>
    /// Adds public-only P-256 verification material under an exact key identifier.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="publicKeyPem">The public key PEM.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="InvalidForwardedIdentityPublicKey">The key is not public P-256 material.</exception>
    /// <exception cref="InvalidVerifiedCallerInstallation">The identifier is duplicated.</exception>
    public SignedForwardedIdentityV2Builder AddPublicKey(string keyId, string publicKeyPem)
    {
        if (!_keys.TryAdd(keyId, ForwardedIdentityV2PublicKey.FromPem(keyId, publicKeyPem)))
        {
            throw new InvalidVerifiedCallerInstallation("A forwarded identity key identifier must be unique.");
        }
        return this;
    }

    /// <summary>
    /// Selects replay protection for one process only; not suitable for a fleet.
    /// </summary>
    /// <returns>This builder.</returns>
    public SignedForwardedIdentityV2Builder UseProcessLocalNonceStore()
    {
        _nonceStore = ServiceDescriptor.Singleton<IForwardedIdentityNonceStore, ProcessLocalForwardedIdentityNonceStore>();
        return this;
    }

    /// <summary>
    /// Selects a product-owned fleet-wide atomic replay store.
    /// </summary>
    /// <typeparam name="TStore">The singleton, concurrency-safe store implementation.</typeparam>
    /// <returns>This builder.</returns>
    public SignedForwardedIdentityV2Builder UseNonceStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>()
        where TStore : class, IForwardedIdentityNonceStore
    {
        _nonceStore = ServiceDescriptor.Singleton<IForwardedIdentityNonceStore, TStore>();
        return this;
    }

    /// <summary>Validates the configuration and snapshots the verification settings.</summary>
    /// <param name="resource">The receiving resource.</param>
    /// <param name="audience">The receiving audience.</param>
    /// <returns>The immutable settings.</returns>
    /// <exception cref="InvalidVerifiedCallerInstallation">Required trust settings are missing.</exception>
    /// <exception cref="ForwardedIdentityNonceStoreNotConfigured">No replay store was selected.</exception>
    internal ForwardedIdentityV2Settings Settings(string resource, string audience)
    {
        if (string.IsNullOrWhiteSpace(_realmIssuer) || _keys.Count == 0)
        {
            throw new InvalidVerifiedCallerInstallation("Signed forwarding requires a realm issuer and public keys.");
        }
        if (_nonceStore is null)
        {
            throw new ForwardedIdentityNonceStoreNotConfigured();
        }
        return new(audience, resource, _realmIssuer, _tenantHeader, _keys, v2Only: true);
    }

    /// <summary>Registers the explicitly selected replay store.</summary>
    /// <param name="services">The receiving service collection.</param>
    internal void InstallNonceStore(IServiceCollection services) => services.Add(_nonceStore!);
}
