// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_a_forwarding_wrapper : given.two_roots
{
    bool _accepted;
    ForwardingProvider _wrapper;

    void Establish()
    {
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out _);
        _wrapper = new(_scope.ServiceProvider);
    }

    void Because() => _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_wrapper, out _);

    [Fact] void should_refuse_the_wrapper() => _accepted.ShouldBeFalse();
    [Fact] void should_not_ask_the_wrapper_for_a_probe() => _wrapper.Resolutions.ShouldEqual(0);

    sealed class ForwardingProvider(IServiceProvider provider) : IServiceProvider
    {
        internal int Resolutions { get; private set; }

        public object? GetService(Type serviceType)
        {
            Resolutions++;
            return provider.GetService(serviceType);
        }
    }
}
