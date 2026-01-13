// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts.given;

public class a_response_phase_database : Specification
{
    protected ResponsePhaseDbContext _context = null!;
    SqliteConnection _connection = null!;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        // Use AddConceptAsSupport() like the actual extension methods do
        // This tests the same configuration path that users will use
        var options = new DbContextOptionsBuilder<ResponsePhaseDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        _context = new ResponsePhaseDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
