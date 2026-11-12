// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_failed_root_disposal : Specification
{
    bool _accepted;
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
        root.GetRequiredService<IDisposable>();
        _error = Catch.Exception(root.Dispose);
        _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out _);
    }

    [Fact] void should_exercise_an_interrupted_disposal() => _error.ShouldBeSame(_failure);
    [Fact] void should_still_refuse_the_child_scope() => _accepted.ShouldBeFalse();
}
