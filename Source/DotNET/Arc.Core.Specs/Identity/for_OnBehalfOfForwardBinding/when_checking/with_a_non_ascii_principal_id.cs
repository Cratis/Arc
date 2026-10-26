// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_OnBehalfOfForwardBinding.when_checking;

public class with_a_non_ascii_principal_id : given.a_forward_for_a_non_ascii_user
{
    string? _encoded;
    string? _verbatim;

    void Because()
    {
        _encoded = OnBehalfOfForwardBinding.Check(_assertion, ForwardWithPrincipalId(EncodedUserId), _represented);
        _verbatim = OnBehalfOfForwardBinding.Check(_assertion, ForwardWithPrincipalId(UserId), _represented);
    }

    [Fact] void should_accept_the_id_as_authproxy_encodes_it() => _encoded.ShouldBeNull();
    [Fact] void should_accept_the_id_verbatim() => _verbatim.ShouldBeNull();
}
