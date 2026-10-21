// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_failed_root_disposal : Specification
{
    bool _accepted;
    bool _sentinelStillAlive;
    Exception? _error;
    Exception _failure;

    void Because()
    {
        _failure = new Exception("Disposal failed");
        var disposable = Substitute.For<IDisposable>();
        disposable.When(value => value.Dispose()).Do(_ => throw _failure);
        var services = new ServiceCollection();
        services.AddSingleton(_ => disposable);
        using var root = (ServiceProvider)new ProtectedDecisionServiceProviderFactory().CreateServiceProvider(services);
        using var scope = root.CreateScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation);
        root.GetRequiredService<IDisposable>();
        _error = Catch.Exception(root.Dispose);
        _sentinelStillAlive = attestation!.IsAlive;
        _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out _);
    }

    [Fact] void should_exercise_an_interrupted_disposal() => _error.ShouldBeSame(_failure);
    [Fact] void should_exercise_a_root_sentinel_that_was_not_disposed() => _sentinelStillAlive.ShouldBeTrue();
    [Fact] void should_still_refuse_the_child_scope() => _accepted.ShouldBeFalse();
}
