// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_copied_provider_options : Specification
{
    Exception? _error;

    void Because()
    {
        var options = new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true };
        var factory = new ProtectedDecisionServiceProviderFactory(options);
        options.ValidateScopes = false;
        options.ValidateOnBuild = false;
        var services = new ServiceCollection();
        services.AddScoped<ScopedDependency>();
        services.AddSingleton<CaptiveDependency>();
        _error = Catch.Exception(() =>
        {
            using var provider = (ServiceProvider)factory.CreateServiceProvider(services);
        });
    }

    [Fact] void should_preserve_eager_scope_validation() => _error.ShouldBeOfExactType<AggregateException>();

    sealed class ScopedDependency;
    sealed class CaptiveDependency(ScopedDependency dependency)
    {
        public ScopedDependency Dependency { get; } = dependency;
    }
}
