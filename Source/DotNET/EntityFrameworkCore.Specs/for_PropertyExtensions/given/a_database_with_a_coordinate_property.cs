// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_PropertyExtensions.given;

public class a_database_with_a_coordinate_property : Specification
{
    protected SqliteConnection _connection;
    protected PlaceDbContext _context;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PlaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new PlaceDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
