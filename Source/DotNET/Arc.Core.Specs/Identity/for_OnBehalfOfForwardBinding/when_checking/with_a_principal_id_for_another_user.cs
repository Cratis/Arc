// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_OnBehalfOfForwardBinding.when_checking;

public class with_a_principal_id_for_another_user : given.a_forward_for_a_non_ascii_user
{
    string? _anotherUser;
    string? _invalidEncoding;
    string? _undecodedLookalike;

    void Because()
    {
        _anotherUser = OnBehalfOfForwardBinding.Check(_assertion, ForwardWithPrincipalId("UTF-8''s%C3%B8ren"), _represented);
        _invalidEncoding = OnBehalfOfForwardBinding.Check(_assertion, ForwardWithPrincipalId("UTF-8''s%C3ren-w%C3%A6rstad"), _represented);
        _undecodedLookalike = OnBehalfOfForwardBinding.Check(_assertion, ForwardWithPrincipalId("s%C3%B8ren-w%C3%A6rstad"), _represented);
    }

    [Fact] void should_reject_another_user() => _anotherUser.ShouldEqual(OnBehalfOfFailures.PrincipalMismatch);
    [Fact] void should_reject_an_invalid_encoding() => _invalidEncoding.ShouldEqual(OnBehalfOfFailures.PrincipalMismatch);
    [Fact] void should_not_decode_a_value_without_the_prefix() => _undecodedLookalike.ShouldEqual(OnBehalfOfFailures.PrincipalMismatch);
}
