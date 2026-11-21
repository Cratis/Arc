// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_resolving_a_parameter_synchronously : given.an_application_parameter_resolver
{
    object? _resolved;

    void Establish() => _services.AddScoped<ICanResolveParameterDependency, ApplicationResolver>();

    void Because() => _resolved = ResolveSynchronously();

    [Fact] void should_consult_the_application_resolver() => _resolved.ShouldBeSame(_dependency.Date);
    [Fact] void should_resolve_from_the_invocation_scope() => _dependency.ResolverInstances.ShouldEqual(1);
}
