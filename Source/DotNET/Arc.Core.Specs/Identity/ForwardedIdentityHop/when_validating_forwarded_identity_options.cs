// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_validating_forwarded_identity_options : Specification
{
    readonly Dictionary<string, bool> _results = [];

    void Because()
    {
        Check("default-off", new ArcOptions());
        Check("default-unsigned", new ArcOptions { TrustForwardedIdentityHeaders = true });
        Check("local-unsigned", new ArcOptions { TrustForwardedIdentityHeaders = true, Profile = DeploymentProfile.LocalDevelopment });
        foreach (var profile in new[] { DeploymentProfile.Default, DeploymentProfile.LocalDevelopment })
        {
            Check($"{profile}-signed", Signed(profile));
            var missingAudience = Signed(profile);
            missingAudience.ForwardedIdentity.Audience = string.Empty;
            Check($"{profile}-missing-audience", missingAudience);
            var missingKey = Signed(profile);
            missingKey.ForwardedIdentity.Keys.Clear();
            Check($"{profile}-missing-key", missingKey);
            var malformedKey = Signed(profile);
            malformedKey.ForwardedIdentity.Keys[0].SecretBase64 = "invalid";
            Check($"{profile}-malformed-key", malformedKey);
            var duplicateKey = Signed(profile);
            duplicateKey.ForwardedIdentity.Keys.Add(duplicateKey.ForwardedIdentity.Keys[0]);
            Check($"{profile}-duplicate-key", duplicateKey);
            var disabled = Signed(profile);
            disabled.TrustForwardedIdentityHeaders = false;
            Check($"{profile}-master-off", disabled);
            var v2 = new ArcOptions { Profile = profile, TrustForwardedIdentityHeaders = true, ForwardedIdentityV2 = true };
            Check($"{profile}-v2-without-hop", v2);
            var contradiction = Signed(profile);
            contradiction.ForwardedIdentityV2 = true;
            Check($"{profile}-v2-only-contradiction", contradiction, new("api", "resource", "issuer", null, new Dictionary<string, ForwardedIdentityV2PublicKey>(), true));
            Check($"{profile}-invalid-hop", new ArcOptions { Profile = profile, ForwardedIdentity = new() { ClientCertificate = new() { Required = true } } });
        }
    }

    [Fact] void should_start_secure_default_without_forwarding() => _results["default-off"].ShouldBeTrue();
    [Fact] void should_refuse_unsigned_forwarding_in_default() => _results["default-unsigned"].ShouldBeFalse();
    [Fact] void should_allow_explicit_local_unsigned_forwarding() => _results["local-unsigned"].ShouldBeTrue();
    [Fact] void should_start_valid_signed_forwarding_in_both_profiles() => Both("signed", true);
    [Fact] void should_refuse_missing_audience_in_both_profiles() => Both("missing-audience", false);
    [Fact] void should_refuse_missing_keys_in_both_profiles() => Both("missing-key", false);
    [Fact] void should_refuse_malformed_keys_in_both_profiles() => Both("malformed-key", false);
    [Fact] void should_refuse_duplicate_keys_in_both_profiles() => Both("duplicate-key", false);
    [Fact] void should_allow_the_master_switch_to_disable_valid_signing() => Both("master-off", true);
    [Fact] void should_refuse_v2_without_hop_in_default() => _results["Default-v2-without-hop"].ShouldBeFalse();
    [Fact] void should_allow_v2_without_hop_at_local_startup() => _results["LocalDevelopment-v2-without-hop"].ShouldBeTrue();
    [Fact] void should_refuse_explicit_v2_only_with_v1_enabled() => Both("v2-only-contradiction", false);
    [Fact] void should_refuse_invalid_required_hop_in_both_profiles() => Both("invalid-hop", false);

    static ArcOptions Signed(DeploymentProfile profile) => new()
    {
        Profile = profile,
        TrustForwardedIdentityHeaders = true,
        ForwardedIdentity = new()
        {
            Enabled = true,
            Audience = "api",
            Keys = [new() { KeyId = "f0", SecretBase64 = Convert.ToBase64String(new byte[32]) }]
        }
    };

    void Check(string name, ArcOptions options, ForwardedIdentityV2Settings? v2 = null)
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ForwardedIdentityV2Settings)).Returns(v2);
        _results[name] = new ForwardedIdentityOptionsValidator(NullLogger<ForwardedIdentityOptionsValidator>.Instance, services).Validate(null, options).Succeeded;
    }

    void Both(string name, bool expected)
    {
        _results[$"Default-{name}"].ShouldEqual(expected);
        _results[$"LocalDevelopment-{name}"].ShouldEqual(expected);
    }
}
