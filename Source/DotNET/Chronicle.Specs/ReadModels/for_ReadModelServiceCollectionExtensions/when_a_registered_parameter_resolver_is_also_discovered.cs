// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_a_registered_parameter_resolver_is_also_discovered : given.an_application_parameter_resolver
{
    ICanResolveParameterDependency[] _resolvers;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanResolveParameterDependency>().Returns([typeof(ChronicleParameterDependencyResolver), typeof(ApplicationResolver)]);
        _services.AddSingleton(types)
            .AddScoped<ApplicationResolver>()
            .AddScoped<ICanResolveParameterDependency, ApplicationResolver>();
    }

    void Because()
    {
        _root = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _root.CreateScope();
        _resolvers = _scope.ServiceProvider.GetRequiredService<IInstancesOf<ICanResolveParameterDependency>>().ToArray();
    }

    [Fact] void should_keep_chronicle_first() => _resolvers[0].ShouldBeSame(_scope.ServiceProvider.GetRequiredService<ChronicleParameterDependencyResolver>());
    [Fact] void should_include_each_resolver_once() => _resolvers.Length.ShouldEqual(2);
    [Fact] void should_not_construct_an_additional_discovered_instance() => _dependency.ResolverInstances.ShouldEqual(1);
}
