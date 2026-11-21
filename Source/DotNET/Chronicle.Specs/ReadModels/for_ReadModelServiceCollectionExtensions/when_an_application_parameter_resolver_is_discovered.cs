// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_an_application_parameter_resolver_is_discovered : given.an_application_parameter_resolver
{
    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanResolveParameterDependency>().Returns([typeof(ChronicleParameterDependencyResolver), typeof(ApplicationResolver)]);
        _services.AddSingleton(types)
            .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>))
            .AddScoped<ApplicationResolver>();
    }

    Task Because() => Resolve();

    [Fact] void should_bind_the_handler_parameter_from_the_application_resolver() => _result.Arguments[0].ShouldBeSame(_dependency.Date);
    [Fact] void should_construct_the_resolver_from_the_invocation_scope() => _dependency.ResolverInstances.ShouldEqual(1);
}
