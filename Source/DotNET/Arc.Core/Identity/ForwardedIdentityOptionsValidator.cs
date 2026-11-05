// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Enforces forwarding safety before a host reports itself started.
/// </summary>
/// <param name="logger">The startup diagnostics logger.</param>
/// <param name="services">The host services, consulted for v2 only after explicit registration.</param>
internal sealed class ForwardedIdentityOptionsValidator(ILogger<ForwardedIdentityOptionsValidator> logger, IServiceProvider services) : IValidateOptions<ArcOptions>
{
    int _reported;

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ArcOptions options)
    {
        var failures = new List<string>();
        var policy = ForwardedIdentityPolicy.Read(options);
        if (!Enum.IsDefined(options.Profile)) failures.Add("Cratis:Arc:Profile is not a supported deployment profile.");
        if (policy.UnsignedV1 && options.Profile != DeploymentProfile.LocalDevelopment)
        {
            failures.Add("Unsigned forwarded identity requires Cratis:Arc:Profile=LocalDevelopment. Configure signed forwarding for deployed hosts.");
        }
        ValidateSigning(options, failures);
        ValidateCertificates(options, failures);
        ValidateV2(options, failures);
        if (failures.Count > 0) return ValidateOptionsResult.Fail(failures);
        if (Interlocked.Exchange(ref _reported, 1) == 0) Report(options, policy);
        return ValidateOptionsResult.Success;
    }

    static bool HasConfiguredForwarding(ArcOptions options) =>
        options.ForwardedIdentity.Enabled || options.ForwardedIdentityV2 || options.ForwardedIdentity.ClientCertificate.Required;

    static void ValidateSigning(ArcOptions options, List<string> failures)
    {
        if (!options.ForwardedIdentity.Enabled) return;
        var snapshot = new ArcOptions
        {
            TrustForwardedIdentityHeaders = true,
            ForwardedIdentity = options.ForwardedIdentity,
            OnBehalfOf = options.OnBehalfOf,
            Tenancy = options.Tenancy
        };
        if (ForwardedIdentityConfiguration.Read(snapshot) is null)
        {
            failures.Add("Enabled signed forwarding requires a valid audience and 1-8 distinct key IDs with canonical base64 32-byte keys.");
        }
    }

    void ValidateV2(ArcOptions options, List<string> failures)
    {
        if (!options.ForwardedIdentityV2) return;
        if (!options.ForwardedIdentity.ClientCertificate.Required && options.Profile != DeploymentProfile.LocalDevelopment)
        {
            failures.Add("Forwarded identity v2 requires ForwardedIdentity:ClientCertificate:Required=true in the default deployment profile.");
        }
        if (options.ForwardedIdentity.Enabled && services.GetService(typeof(ForwardedIdentityV2Settings)) is ForwardedIdentityV2Settings { V2Only: true })
        {
            failures.Add("Forwarded identity V2Only cannot be combined with enabled v1 signing.");
        }
    }

    void ValidateCertificates(ArcOptions options, List<string> failures)
    {
        var certificate = options.ForwardedIdentity.ClientCertificate;
        if (!certificate.Required) return;
        if (!ForwardedIdentityClientCertificateTrust.IsConfigured(certificate))
        {
            failures.Add("Required forwarding certificates need a CA bundle, non-wildcard DNS SAN allowlist, and reload interval of at least 5 seconds.");
            return;
        }
        if (!ForwardedIdentityClientCertificateTrust.For(certificate.TrustedCertificateAuthoritiesPath, TimeProvider.System).Load(certificate.ReloadInterval, logger))
        {
            failures.Add("Required forwarding client-certificate CA bundle is unavailable or invalid.");
        }
    }

    void Report(ArcOptions options, ForwardedIdentityPolicy policy)
    {
        if (policy.UnsignedV1) logger.UnsignedLocalDevelopment();
        if (policy.SignedV1 && !options.ForwardedIdentity.ClientCertificate.Required) logger.SignedForwardingWithoutCertificate();
        if (options.ForwardedIdentityV2 && !options.ForwardedIdentity.ClientCertificate.Required) logger.V2WithoutCertificateLocalDevelopment();
        if (!options.TrustForwardedIdentityHeaders && HasConfiguredForwarding(options)) logger.ForwardingDisabledByMasterSwitch();
        logger.ForwardingPolicyAtStartup("Requests", options.Profile, policy.UnsignedV1, policy.SignedV1, policy.V2, options.ForwardedIdentity.ClientCertificate.Required);
        logger.ForwardingPolicyAtStartup("IdentityDetails", options.Profile, policy.UnsignedV1, policy.SignedV1, policy.V2, options.ForwardedIdentity.ClientCertificate.Required);
        logger.ForwardingPolicyAtStartup("TenantList", options.Profile, policy.UnsignedV1, policy.SignedV1, policy.V2, options.ForwardedIdentity.ClientCertificate.Required);
    }
}
