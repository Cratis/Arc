// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.SqlServer.for_DbSetExtensions.given;

public class an_upsert_database(SqlServerFixture fixture) : Specification
{
    protected DbContextOptions<UpsertDbContext> _options;
    protected UpsertDbContext _context;
    protected UpsertItem _item;

    async Task Establish()
    {
        _options = new DbContextOptionsBuilder<UpsertDbContext>().UseSqlServer(new SqlConnectionStringBuilder(fixture.ConnectionString) { InitialCatalog = $"upsert_{Guid.NewGuid():N}" }.ConnectionString).Options;
        _context = new UpsertDbContext(_options);
        await _context.Database.EnsureCreatedAsync();
        _item = new UpsertItem { Tenant = "tenant'one", Id = Guid.NewGuid(), Value = new("first ' ; -- value"), Optional = "before" };
    }

    async Task Destroy()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
}
