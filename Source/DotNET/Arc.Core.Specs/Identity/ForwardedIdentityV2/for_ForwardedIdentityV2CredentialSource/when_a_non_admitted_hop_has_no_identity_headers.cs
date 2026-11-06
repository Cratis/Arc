// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_a_non_admitted_hop_has_no_identity_headers : given.a_hop_reader
{
    void Establish() => _headers.Clear();

    Task Because() => _source.Verify(_presentation, CancellationToken.None);

    [Fact] void should_not_log_a_hop_warning() => _warnings.ShouldBeEmpty();
}
