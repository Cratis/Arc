// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_parameter_resolvers_are_requested_in_multiple_scopes : given.an_application_parameter_resolver
{
    ITypes _types;
    ICanResolveParameterDependency _first;
    ICanResolveParameterDependency _second;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.FindMultiple<ICanResolveParameterDependency>().Returns([typeof(ApplicationResolver)]);
        _services.AddSingleton(_types).AddScoped<ApplicationResolver>();
    }

    void Because()
    {
        BuildScope();
        _first = _scope.ServiceProvider.GetRequiredService<IInstancesOf<ICanResolveParameterDependency>>().Last();
        using var secondScope = _root.CreateScope();
        _second = secondScope.ServiceProvider.GetRequiredService<IInstancesOf<ICanResolveParameterDependency>>().Last();
    }

    [Fact] void should_discover_types_only_once() => _types.Received(1).FindMultiple<ICanResolveParameterDependency>();
    [Fact] void should_resolve_a_different_instance_per_scope() => _second.ShouldNotBeSame(_first);
    [Fact] void should_construct_both_scoped_instances() => _dependency.ResolverInstances.ShouldEqual(2);
}
