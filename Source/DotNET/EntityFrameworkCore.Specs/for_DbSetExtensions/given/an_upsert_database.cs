// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.given;

public class an_upsert_database : Specification
{
    protected DbContextOptions<UpsertDbContext> _options;
    protected UpsertDbContext _context;
    protected UpsertItem _item;

    async Task Establish()
    {
        _options = new DbContextOptionsBuilder<UpsertDbContext>().UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"arc-upsert-{Guid.NewGuid():N}.db")}").Options;
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
