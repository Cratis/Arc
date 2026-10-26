// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_OnBehalfOfSigningKeys.when_reading_keys;

public class with_a_public_key : Specification
{
    OnBehalfOfSigningKeys _keys;

    void Because() => _keys = OnBehalfOfSigningKeys.For(OnBehalfOfAssertionsForSpecs.Options());

    [Fact] void should_accept_the_configuration() => _keys.Problem.ShouldBeNull();
    [Fact] void should_find_the_key_by_its_identifier() => _keys.TryGet(OnBehalfOfAssertionsForSpecs.KeyId, out _).ShouldBeTrue();
    [Fact] void should_not_find_another_key() => _keys.TryGet("another-key", out _).ShouldBeFalse();
}
