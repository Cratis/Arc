// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>Validates forwarding peers against a reloadable, exclusively private trust bundle.</summary>
/// <param name="options">The host options.</param>
/// <param name="logger">The diagnostics logger.</param>
internal sealed class ForwardedIdentityClientCertificateTrustStore(IOptionsMonitor<ArcOptions> options, ILogger<ForwardedIdentityClientCertificateTrustStore> logger) : IDisposable
{
    readonly object _gate = new();
    X509Certificate2Collection _authorities = [];
    string? _path;
    DateTimeOffset _nextReload;
    bool _usable;

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            DisposeCertificates(_authorities);
            _authorities = [];
            _usable = false;
        }
    }

    /// <summary>Checks required configuration and loads the initial authority bundle.</summary>
    /// <exception cref="OptionsValidationException">Required configuration is unavailable or invalid.</exception>
    internal void ValidateConfiguration()
    {
        lock (_gate)
        {
            var configured = options.CurrentValue.ForwardedIdentity.ClientCertificate;
            if (!configured.Required) return;
            if (!IsConfigured(configured))
            {
                throw new OptionsValidationException(nameof(ArcOptions), typeof(ArcOptions), ["Forwarded identity client-certificate admission requires a CA bundle, exact DNS SAN allowlist, and positive reload interval."]);
            }
            Reload(configured);
            if (!_usable) throw new OptionsValidationException(nameof(ArcOptions), typeof(ArcOptions), ["Forwarded identity client-certificate trust bundle is unavailable or invalid."]);
        }
    }

    /// <summary>Checks the immediate TLS peer against current private trust and identity policy.</summary>
    /// <param name="certificate">The actual TLS peer certificate.</param>
    /// <returns>Whether the peer is admitted.</returns>
    internal bool IsTrusted(X509Certificate2 certificate)
    {
        lock (_gate)
        {
            var configured = options.CurrentValue.ForwardedIdentity.ClientCertificate;
            if (!configured.Required) return false;
            if (_path != configured.TrustedCertificateAuthoritiesPath || DateTimeOffset.UtcNow >= _nextReload) Reload(configured);
            if (!_usable || !IsConfigured(configured)) return false;
            try
            {
                return HasClientIdentity(certificate, configured.AllowedDnsNames) && BuildsTrustedChain(certificate);
            }
            catch (Exception error) when (error is CryptographicException or InvalidOperationException)
            {
                return false;
            }
        }
    }

    static bool IsConfigured(ForwardedIdentityClientCertificateOptions configured) =>
        !string.IsNullOrWhiteSpace(configured.TrustedCertificateAuthoritiesPath) && HasAllowedNames(configured) && configured.ReloadInterval > TimeSpan.Zero;

    static bool HasAllowedNames(ForwardedIdentityClientCertificateOptions configured) =>
        configured.AllowedDnsNames.Count > 0 && configured.AllowedDnsNames.All(name => !string.IsNullOrWhiteSpace(name) && !name.Contains('*'));

    static bool HasClientIdentity(X509Certificate2 certificate, IList<string> allowedNames)
    {
        var eku = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().SingleOrDefault();
        if (eku?.EnhancedKeyUsages.Cast<Oid>().Any(usage => usage.Value == "1.3.6.1.5.5.7.3.2") != true) return false;
        var san = certificate.Extensions.SingleOrDefault(extension => extension.Oid?.Value == "2.5.29.17");
        return san is not null && new X509SubjectAlternativeNameExtension(san.RawData, san.Critical).EnumerateDnsNames().Any(name => allowedNames.Contains(name, StringComparer.Ordinal));
    }

    static void ReadAuthorities(string path, X509Certificate2Collection certificates)
    {
        var remaining = File.ReadAllText(path).AsSpan();
        while (!remaining.Trim().IsEmpty)
        {
            if (!PemEncoding.TryFind(remaining, out var fields)) throw new CryptographicException();
            if (!remaining[..fields.Location.Start].Trim().IsEmpty) throw new CryptographicException();
            if (remaining[fields.Label] is not "CERTIFICATE") throw new CryptographicException();
            certificates.Add(X509Certificate2.CreateFromPem(remaining[fields.Location]));
            remaining = remaining[fields.Location.End..];
        }
    }

    static void DisposeCertificates(X509Certificate2Collection certificates)
    {
        foreach (var certificate in certificates) certificate.Dispose();
    }

    void Reload(ForwardedIdentityClientCertificateOptions configured)
    {
        var replacement = new X509Certificate2Collection();
        var usable = false;
        try
        {
            ReadAuthorities(configured.TrustedCertificateAuthoritiesPath!, replacement);
            usable = replacement.Count > 0 && replacement.All(certificate => certificate.Extensions.OfType<X509BasicConstraintsExtension>().Any(constraint => constraint.CertificateAuthority));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException or ArgumentException)
        {
            // Never retain stale trust after a failed replacement, or log certificate/path contents.
        }
        DisposeCertificates(_authorities);
        _authorities = replacement;
        _path = configured.TrustedCertificateAuthoritiesPath;
        _usable = usable;
        _nextReload = DateTimeOffset.UtcNow + (configured.ReloadInterval > TimeSpan.Zero ? configured.ReloadInterval : TimeSpan.FromMinutes(1));
        if (!usable) logger.TrustBundleUnavailable();
    }

    bool BuildsTrustedChain(X509Certificate2 certificate)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(_authorities);
        chain.ChainPolicy.ExtraStore.AddRange(_authorities);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.DisableCertificateDownloads = true;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
        return chain.Build(certificate);
    }
}
