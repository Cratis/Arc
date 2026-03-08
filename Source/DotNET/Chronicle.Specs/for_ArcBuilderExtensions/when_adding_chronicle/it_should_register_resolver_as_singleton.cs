// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_adding_chronicle;

public class it_should_register_resolver_as_singleton : given.an_arc_builder
{
    void Because() => _builder.WithChronicle();

    [Fact] void should_register_resolver_as_singleton() =>
        _services.ShouldContain(_ => _.ServiceType == typeof(ICommandDependencyResolver) && _.Lifetime == ServiceLifetime.Singleton);
}
