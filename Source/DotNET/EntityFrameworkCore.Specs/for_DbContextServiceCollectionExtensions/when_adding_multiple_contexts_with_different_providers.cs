// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.for_DbContextServiceCollectionExtensions;

public class when_adding_multiple_contexts_with_different_providers : Specification
{
    IServiceCollection _services;
    IServiceProvider _serviceProvider;

    void Establish()
    {
        _services = new ServiceCollection();

        _services.AddDbContextWithConnectionString<SqliteTestContext>("Data Source=:memory:");
        _services.AddDbContextWithConnectionString<SqlServerTestContext>("Server=localhost;Database=test;Trusted_Connection=true");
    }

    void Because() => _serviceProvider = _services.BuildServiceProvider();

    [Fact] void should_be_able_to_resolve_sqlite_context() => _serviceProvider.GetService<SqliteTestContext>().ShouldNotBeNull();
    [Fact] void should_be_able_to_resolve_sqlserver_context() => _serviceProvider.GetService<SqlServerTestContext>().ShouldNotBeNull();
    [Fact] void should_be_able_to_resolve_sqlite_context_factory() => _serviceProvider.GetService<IDbContextFactory<SqliteTestContext>>().ShouldNotBeNull();
    [Fact] void should_be_able_to_resolve_sqlserver_context_factory() => _serviceProvider.GetService<IDbContextFactory<SqlServerTestContext>>().ShouldNotBeNull();
}

