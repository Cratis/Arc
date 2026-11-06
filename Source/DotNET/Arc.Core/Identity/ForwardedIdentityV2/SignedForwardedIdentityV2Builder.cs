// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Configures public verification keys and explicit replay protection for a v2-only browser hop.</summary>
public sealed class SignedForwardedIdentityV2Builder
{
    readonly Dictionary<string, ForwardedIdentityV2PublicKey> _keys = new(StringComparer.Ordinal);
    string? _realmIssuer;
    string? _tenantHeader;
    ServiceDescriptor? _nonceStore;

    /// <summary>Sets the exact trusted account issuer.</summary>
    /// <param name="realmIssuer">The trusted issuer.</param>
    /// <returns>This builder.</returns>
    public SignedForwardedIdentityV2Builder WithRealmIssuer(string realmIssuer)
    {
        _realmIssuer = realmIssuer;
        return this;
    }

    /// <summary>Includes a custom tenant header in signature coverage.</summary>
    /// <param name="tenantHeader">The raw tenant header name.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException">The header name is invalid.</exception>
    public SignedForwardedIdentityV2Builder WithTenantHeader(string tenantHeader)
    {
        if (string.IsNullOrWhiteSpace(tenantHeader) || tenantHeader.Any(c => !char.IsAsciiLetterOrDigit(c) && !"!#$%&'*+-.^_`|~".Contains(c)))
        {
            throw new ArgumentException("A tenant header must be an HTTP field name.", nameof(tenantHeader));
        }
        _tenantHeader = tenantHeader;
        return this;
    }

    /// <summary>Adds public-only P-256 verification material under an exact key identifier.</summary>
    /// <param name="kid">The key identifier.</param>
    /// <param name="pem">The public key PEM.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException">The key is invalid or the identifier is duplicated.</exception>
    public SignedForwardedIdentityV2Builder AddPublicKey(string kid, string pem)
    {
        if (!_keys.TryAdd(kid, ForwardedIdentityV2PublicKey.FromPem(kid, pem)))
        {
            throw new ArgumentException("A forwarded identity key identifier must be unique.", nameof(kid));
        }
        return this;
    }

    /// <summary>Selects replay protection for one process only; not suitable for a fleet.</summary>
    /// <returns>This builder.</returns>
    public SignedForwardedIdentityV2Builder UseProcessLocalNonceStore()
    {
        _nonceStore = ServiceDescriptor.Singleton<IForwardedIdentityNonceStore, ProcessLocalForwardedIdentityNonceStore>();
        return this;
    }

    /// <summary>Selects a product-owned fleet-wide atomic replay store.</summary>
    /// <typeparam name="TStore">The singleton, concurrency-safe store implementation.</typeparam>
    /// <returns>This builder.</returns>
    public SignedForwardedIdentityV2Builder UseNonceStore<TStore>()
        where TStore : class, IForwardedIdentityNonceStore
    {
        _nonceStore = ServiceDescriptor.Singleton<IForwardedIdentityNonceStore, TStore>();
        return this;
    }

    /// <summary>Validates the configuration and snapshots the verification settings.</summary>
    /// <param name="resource">The receiving resource.</param>
    /// <param name="audience">The receiving audience.</param>
    /// <returns>The immutable settings.</returns>
    /// <exception cref="InvalidOperationException">Required trust or replay settings are missing.</exception>
    internal ForwardedIdentityV2Settings Settings(string resource, string audience)
    {
        if (string.IsNullOrWhiteSpace(_realmIssuer) || _keys.Count == 0 || _nonceStore is null)
        {
            throw new InvalidOperationException("Signed forwarding requires a realm issuer, public keys and an explicit nonce store.");
        }
        return new(audience, resource, _realmIssuer, _tenantHeader, _keys, v2Only: true);
    }

    /// <summary>Registers the explicitly selected replay store.</summary>
    /// <param name="services">The receiving service collection.</param>
    internal void InstallNonceStore(IServiceCollection services) => services.Add(_nonceStore!);
}
