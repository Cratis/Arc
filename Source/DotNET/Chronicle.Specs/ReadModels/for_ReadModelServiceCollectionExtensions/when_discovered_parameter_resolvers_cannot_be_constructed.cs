// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_discovered_parameter_resolvers_cannot_be_constructed : given.an_application_parameter_resolver
{
    object? _resolved;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        var openGeneric = Substitute.For<Type>();
        openGeneric.ContainsGenericParameters.Returns(true);
        types.FindMultiple<ICanResolveParameterDependency>().Returns([typeof(AbstractResolver), openGeneric, typeof(ApplicationResolver)]);
        _services.AddSingleton(types).AddScoped<ApplicationResolver>();
    }

    void Because() => _resolved = ResolveSynchronously();

    [Fact] void should_skip_abstract_and_open_generic_resolvers() => _resolved.ShouldBeSame(_dependency.Date);

    public abstract class AbstractResolver : ICanResolveParameterDependency
    {
        public abstract bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency);
    }
}
