// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_deriving_forwarded_identity_policy : Specification
{
    ForwardedIdentityPolicy _off;
    ForwardedIdentityPolicy _unsigned;
    ForwardedIdentityPolicy _signed;
    ForwardedIdentityPolicy _v2;
    ForwardedIdentityPolicy _both;
    DeploymentProfile _default;

    void Because()
    {
        var options = new ArcOptions();
        _default = options.Profile;
        options.ForwardedIdentityV2 = true;
        options.ForwardedIdentity.Enabled = true;
        _off = ForwardedIdentityPolicy.Read(options);
        options.TrustForwardedIdentityHeaders = true;
        options.ForwardedIdentity.Enabled = false;
        options.ForwardedIdentityV2 = false;
        _unsigned = ForwardedIdentityPolicy.Read(options);
        options.ForwardedIdentity.Enabled = true;
        _signed = ForwardedIdentityPolicy.Read(options);
        options.ForwardedIdentity.Enabled = false;
        options.ForwardedIdentityV2 = true;
        _v2 = ForwardedIdentityPolicy.Read(options);
        options.ForwardedIdentity.Enabled = true;
        _both = ForwardedIdentityPolicy.Read(options);
    }

    [Fact] void should_default_the_profile_independently_of_environment() => _default.ShouldEqual(DeploymentProfile.Default);
    [Fact] void should_disable_every_form_with_the_master_switch() => _off.ShouldEqual(new ForwardedIdentityPolicy(false, false, false));
    [Fact] void should_not_silently_clamp_the_profile_at_runtime() => _unsigned.ShouldEqual(new ForwardedIdentityPolicy(true, false, false));
    [Fact] void should_disable_unsigned_with_signed_v1() => _signed.ShouldEqual(new ForwardedIdentityPolicy(false, true, false));
    [Fact] void should_disable_unsigned_with_v2() => _v2.ShouldEqual(new ForwardedIdentityPolicy(false, false, true));
    [Fact] void should_allow_explicit_signed_coexistence() => _both.ShouldEqual(new ForwardedIdentityPolicy(false, true, true));
}
