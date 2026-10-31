// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_capturing_configuration : Specification
{
    ArcOptions _options;
    string _original;
    string _unchanged;
    string _rotated;

    void Establish() => _options = ForwardedIdentityForSpecs.Options();

    void Because()
    {
        _original = ForwardedIdentityConfiguration.Read(_options).Snapshot;
        _unchanged = ForwardedIdentityConfiguration.Read(_options).Snapshot;
        _options.ForwardedIdentity.Keys[0].SecretBase64 = Convert.ToBase64String(new byte[32]);
        _rotated = ForwardedIdentityConfiguration.Read(_options).Snapshot;
    }

    [Fact] void should_not_retain_the_original_secret_text() => _original.ShouldNotContain(Convert.ToBase64String(ForwardedIdentityForSpecs.Key));
    [Fact] void should_not_retain_the_rotated_secret_text() => _rotated.ShouldNotContain(_options.ForwardedIdentity.Keys[0].SecretBase64);
    [Fact] void should_match_unchanged_key_material() => _unchanged.ShouldEqual(_original);
    [Fact] void should_invalidate_the_snapshot_on_rotation_under_the_same_key_id() => _rotated.ShouldNotEqual(_original);
}
