// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

/// <summary>
/// Retains the last good private authority bundle and bounded chain results for a path.
/// </summary>
/// <param name="path">The private CA bundle path.</param>
/// <param name="clock">The reload and chain clock.</param>
internal sealed class ForwardedIdentityClientCertificateTrust(string path, TimeProvider clock)
{
    const int MaximumBundleBytes = 1024 * 1024;
    static readonly ConcurrentDictionary<string, ForwardedIdentityClientCertificateTrust> _stores = new(StringComparer.Ordinal);
    readonly object _gate = new();
    readonly Dictionary<(string Fingerprint, long Generation), (bool Trusted, DateTime Expires)> _chains = [];
    X509Certificate2Collection _authorities = [];
    DateTimeOffset _nextReload;
    string? _hash;
    long _generation;

    /// <summary>Gets the successfully loaded bundle generation.</summary>
    internal long Generation
    {
        get { lock (_gate) return _generation; }
    }

    /// <summary>Gets the process-wide trust cache for a configured path.</summary>
    /// <param name="path">The bundle path.</param>
    /// <param name="clock">The reload clock.</param>
    /// <returns>The shared trust cache.</returns>
    internal static ForwardedIdentityClientCertificateTrust For(string? path, TimeProvider clock) =>
        _stores.GetOrAdd(path ?? string.Empty, static (value, time) => new(value, time), clock);

    /// <summary>Checks that required admission has usable policy settings.</summary>
    /// <param name="options">The configured policy.</param>
    /// <returns>Whether all required settings are present.</returns>
    internal static bool IsConfigured(ForwardedIdentityClientCertificateOptions options) =>
        !string.IsNullOrWhiteSpace(options.TrustedCertificateAuthoritiesPath) && options.ReloadInterval >= TimeSpan.FromSeconds(5) &&
        options.AllowedDnsNames.Count > 0 && options.AllowedDnsNames.All(name => !string.IsNullOrWhiteSpace(name) && !name.Contains('*'));

    /// <summary>Loads the initial bundle for startup validation.</summary>
    /// <param name="interval">The reload interval.</param>
    /// <param name="logger">The diagnostics logger.</param>
    /// <returns>Whether a usable bundle has been loaded.</returns>
    internal bool Load(TimeSpan interval, ILogger logger)
    {
        lock (_gate)
        {
            Reload(interval, logger);
            return _authorities.Count > 0;
        }
    }

    /// <summary>Checks a transport certificate against private trust and DNS SAN policy.</summary>
    /// <param name="certificate">The immediate peer certificate.</param>
    /// <param name="options">The admission policy.</param>
    /// <param name="logger">The diagnostics logger.</param>
    /// <returns>The admission decision.</returns>
    internal ForwardedIdentityHopAdmission Admit(X509Certificate2 certificate, ForwardedIdentityClientCertificateOptions options, ILogger logger)
    {
        lock (_gate)
        {
            if (!IsConfigured(options)) return ForwardedIdentityHopAdmission.UntrustedCertificate;
            if (clock.GetUtcNow() >= _nextReload) Reload(options.ReloadInterval, logger);
            if (_authorities.Count == 0 || !BuildsTrustedChain(certificate)) return ForwardedIdentityHopAdmission.UntrustedCertificate;
            return options.AllowedDnsNames.Any(name => certificate.MatchesHostname(name, allowWildcards: false, allowCommonName: false))
                ? ForwardedIdentityHopAdmission.Authenticated
                : ForwardedIdentityHopAdmission.UnexpectedIdentity;
        }
    }

    static byte[] ReadBounded(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > MaximumBundleBytes) throw new IOException("Forwarded identity CA bundle exceeds 1 MiB.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new IOException("Forwarded identity CA bundle grew while reading.");
        return bytes;
    }

    static X509Certificate2Collection ReadAuthorities(byte[] bytes)
    {
        var certificates = new X509Certificate2Collection();
        try
        {
            certificates.ImportFromPem(System.Text.Encoding.UTF8.GetString(bytes));
            if (certificates.Count == 0 || certificates.Any(certificate => !certificate.Extensions.OfType<X509BasicConstraintsExtension>().Any(constraint => constraint.CertificateAuthority)))
            {
                throw new CryptographicException("Forwarded identity CA bundle contains no usable authorities.");
            }
            return certificates;
        }
        catch
        {
            foreach (var certificate in certificates) certificate.Dispose();
            throw;
        }
    }

    void Reload(TimeSpan interval, ILogger logger)
    {
        _nextReload = clock.GetUtcNow() + (interval >= TimeSpan.FromSeconds(5) ? interval : TimeSpan.FromSeconds(5));
        string? hash = null;
        try
        {
            var bytes = ReadBounded(path);
            hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (hash == _hash) return;
            var replacement = ReadAuthorities(bytes);
            foreach (var certificate in _authorities) certificate.Dispose();
            _authorities = replacement;
            _hash = hash;
            _generation++;
            _chains.Clear();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException or ArgumentException)
        {
            logger.TrustBundleUnavailable(path, hash ?? "unavailable");
        }
    }

    bool BuildsTrustedChain(X509Certificate2 certificate)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        if (now < certificate.NotBefore.ToUniversalTime() || now > certificate.NotAfter.ToUniversalTime()) return false;
        var key = (certificate.GetCertHashString(HashAlgorithmName.SHA256), _generation);
        if (_chains.TryGetValue(key, out var cached) && now < cached.Expires) return cached.Trusted;
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(_authorities);
        chain.ChainPolicy.ExtraStore.AddRange(_authorities);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.DisableCertificateDownloads = true;
        chain.ChainPolicy.VerificationTime = now;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
        var trusted = chain.Build(certificate);
        if (_chains.Count >= 64) _chains.Remove(_chains.Keys.First());
        var expires = trusted ? chain.ChainElements.Min(element => element.Certificate.NotAfter.ToUniversalTime()) : now.AddSeconds(5);
        _chains[key] = (trusted, expires);
        return trusted;
    }
}
