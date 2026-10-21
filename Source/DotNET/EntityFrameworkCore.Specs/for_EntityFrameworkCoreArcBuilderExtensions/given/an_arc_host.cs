// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkCoreArcBuilderExtensions.given;

public class an_arc_host : Specification
{
    protected ArcApplicationBuilder _builder;
    protected ConnectionResolutionDbContext _context;
    ServiceProvider _provider;
    AsyncServiceScope? _scope;

    void Establish() => _builder = new ArcApplicationBuilder();

    protected async Task<int> Query()
    {
        _provider = _builder.Services.BuildServiceProvider();
        _scope = _provider.CreateAsyncScope();
        _context = _scope.Value.ServiceProvider.GetRequiredService<ConnectionResolutionDbContext>();
        return await _context.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync();
    }

    async Task Destroy()
    {
        if (_scope is { } scope) await scope.DisposeAsync();
        if (_provider is not null) await _provider.DisposeAsync();
    }
}
