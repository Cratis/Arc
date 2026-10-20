// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Connections;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkCoreArcBuilderExtensions;

public class when_using_only_a_connection_string : given.an_arc_host
{
    int _result;

    void Establish() => _builder.AddCratisArc(configureBuilder: arc => arc.WithEntityFrameworkCore(
        configureOptions: options => options.ConnectionString = "Data Source=:memory:"));

    async Task Because() => _result = await Query();

    [Fact] void should_query_the_read_only_context() => _result.ShouldEqual(1);

    [Fact] void should_register_the_connection_resolution_factory() =>
        _builder.Services.Single(_ => _.ServiceType == typeof(EntityFrameworkCoreConnectionResolution)).ImplementationFactory.ShouldNotBeNull();

    [Fact] void should_not_convention_bind_the_interceptor() =>
        _builder.Services.Where(_ => _.ServiceType == typeof(ConnectionResolutionInterceptor)).ShouldBeEmpty();
}
