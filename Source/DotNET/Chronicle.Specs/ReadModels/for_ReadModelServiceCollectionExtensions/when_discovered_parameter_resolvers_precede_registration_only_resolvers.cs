// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_discovered_parameter_resolvers_precede_registration_only_resolvers : given.an_application_parameter_resolver
{
    ICanResolveParameterDependency[] _resolvers;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanResolveParameterDependency>().Returns([typeof(ApplicationResolver)]);
        _services.AddSingleton(types)
            .AddScoped<IAsyncParameterDependencyResolver, AsyncApplicationResolver>()
            .AddScoped<ApplicationResolver>();
    }

    void Because()
    {
        BuildScope();
        _resolvers = _scope.ServiceProvider.GetRequiredService<IInstancesOf<ICanResolveParameterDependency>>().ToArray();
    }

    [Fact] void should_keep_chronicle_first() => _resolvers[0].ShouldBeOfExactType<ChronicleParameterDependencyResolver>();
    [Fact] void should_consult_discovered_resolvers_next() => _resolvers[1].ShouldBeOfExactType<ApplicationResolver>();
    [Fact] void should_consult_registration_only_resolvers_last() => _resolvers[2].ShouldBeOfExactType<AsyncApplicationResolver>();
}
