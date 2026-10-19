// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_root_providers : given.two_roots
{
    bool _rootAccepted;
    bool _canonicalAccepted;

    void Because()
    {
        _rootAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_first, out _);
        _canonicalAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_first.GetRequiredService<IServiceProvider>(), out _);
    }

    [Fact] void should_refuse_the_returned_root() => _rootAccepted.ShouldBeFalse();
    [Fact] void should_refuse_the_canonical_root_alias() => _canonicalAccepted.ShouldBeFalse();
}
