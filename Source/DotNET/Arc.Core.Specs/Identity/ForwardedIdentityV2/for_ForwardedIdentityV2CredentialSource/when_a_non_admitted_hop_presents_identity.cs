// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_a_non_admitted_hop_presents_identity : given.a_hop_reader
{
    async Task Because()
    {
        _result = await _source.Verify(_presentation, CancellationToken.None);
        await _source.Verify(_presentation, CancellationToken.None);
    }

    [Fact] void should_refuse_the_identity() => _result.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_rate_limit_the_hop_warning() => _warnings.Count.ShouldEqual(1);
    [Fact] void should_use_the_existing_hop_diagnostic() => _warnings[0].ShouldContain("Forwarded identity refused: NoClientCertificate");
    [Fact] void should_not_log_identity_header_values() => _warnings[0].ShouldNotContain("must-not-be-logged");
}
