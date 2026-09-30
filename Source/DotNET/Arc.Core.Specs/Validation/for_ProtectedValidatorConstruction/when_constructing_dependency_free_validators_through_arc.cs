// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation.for_DiscoverableValidators;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction;

public class when_constructing_dependency_free_validators_through_arc : Specification
{
    IServiceCollection _services;
    ServiceDescriptor _dependencyFree = null!;
    ServiceDescriptor _withDependencies = null!;
    object _resolved = null!;

    void Establish()
    {
        _services = new ServiceCollection()
            .AddScoped<CommandWithoutDependenciesValidator>()
            .AddTransient<CommandWithDependencyValidator>()
            .AddSingleton(new CommandDependency());
    }

    void Because()
    {
        _services.ConstructDependencyFreeValidatorsThroughArc(Cratis.Types.Types.Instance);
        _dependencyFree = _services.Single(_ => _.ServiceType == typeof(CommandWithoutDependenciesValidator));
        _withDependencies = _services.Single(_ => _.ServiceType == typeof(CommandWithDependencyValidator));
        using var provider = _services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        _resolved = scope.ServiceProvider.GetRequiredService<CommandWithoutDependenciesValidator>();
    }

    [Fact] void should_construct_the_dependency_free_validator_through_arc() => _dependencyFree.ImplementationFactory.ShouldNotBeNull();
    [Fact] void should_keep_the_lifetime_of_the_binding() => _dependencyFree.Lifetime.ShouldEqual(ServiceLifetime.Scoped);
    [Fact] void should_certify_what_the_container_resolves() => ProtectedValidatorConstruction.IsCertified(_resolved, typeof(CommandWithoutDependenciesValidator)).ShouldBeTrue();
    [Fact] void should_leave_a_validator_with_dependencies_to_the_container() => _withDependencies.ImplementationType.ShouldEqual(typeof(CommandWithDependencyValidator));
}
