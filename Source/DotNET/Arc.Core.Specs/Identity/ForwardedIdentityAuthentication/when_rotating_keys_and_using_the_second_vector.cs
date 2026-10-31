// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_rotating_keys_and_using_the_second_vector : given.a_signed_request
{
    ForwardedIdentityAuthenticationResult _secondVector;
    ForwardedIdentityAuthenticationResult _newKey;
    ForwardedIdentityAuthenticationResult _oldKey;
    ForwardedIdentityAuthenticationResult _removedKey;
    string _envelope;

    void Because()
    {
        Authenticate().Principal.ShouldNotBeNull();
        _context.Items.Returns(new Dictionary<object, object?>());
        _envelope = ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000, nonce: "AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE");
        _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [_envelope];
        _secondVector = Authenticate();
        var key = Enumerable.Repeat((byte)7, 32).ToArray();
        _options.ForwardedIdentity.Keys.Add(new() { KeyId = "k2", SecretBase64 = Convert.ToBase64String(key) });
        _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000, key: key, keyId: "k2")];
        _newKey = Authenticate();
        _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000)];
        _oldKey = Authenticate();
        _options.ForwardedIdentity.Keys.RemoveAt(0);
        _rawHeaders[ForwardedIdentityForSpecs.SignatureHeader] = [ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000)];
        _removedKey = Authenticate();
    }

    [Fact] void should_match_the_independent_second_vector() => _envelope.ShouldEqual("v1.k1.1700000000.AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE.ql7vBinbDwBTqcRq5op-eA4hyMWHf0LFh5MqPlG6qsI");
    [Fact] void should_accept_a_different_nonce_on_a_distinct_request() => _secondVector.Principal.ShouldNotBeNull();
    [Fact] void should_accept_the_new_key() => _newKey.Principal.ShouldNotBeNull();
    [Fact] void should_accept_the_old_key_during_overlap() => _oldKey.Principal.ShouldNotBeNull();
    [Fact] void should_not_fall_back_when_the_old_key_is_removed() => _removedKey.Principal.ShouldBeNull();
}
