// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_hop_admission_is_disabled : given.a_hop_reader
{
    void Establish() => _options.ForwardedIdentity.ClientCertificate.Required = false;

    async Task Because() => _result = await _source.Verify(_presentation, CancellationToken.None);

    [Fact] void should_preserve_the_existing_v2_refusal() => _result.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_not_add_a_hop_warning() => _warnings.ShouldBeEmpty();
}
