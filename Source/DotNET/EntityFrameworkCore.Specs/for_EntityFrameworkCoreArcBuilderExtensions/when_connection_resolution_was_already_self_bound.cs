// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkCoreArcBuilderExtensions;

public class when_connection_resolution_was_already_self_bound : given.an_arc_host
{
    int _result;

    void Establish()
    {
        _builder.Services.AddSingleton<EntityFrameworkCoreConnectionResolution>();
        _builder.AddCratisArc(configureBuilder: arc => arc.WithEntityFrameworkCore(
            configureOptions: options => options.ConnectionString = "Data Source=:memory:"));
    }

    async Task Because() => _result = await Query();

    [Fact] void should_query_the_read_only_context() => _result.ShouldEqual(1);

    [Fact] void should_replace_the_self_binding_with_the_factory() =>
        _builder.Services.Single(_ => _.ServiceType == typeof(EntityFrameworkCoreConnectionResolution)).ImplementationFactory.ShouldNotBeNull();
}
