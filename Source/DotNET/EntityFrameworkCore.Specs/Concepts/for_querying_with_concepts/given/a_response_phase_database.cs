// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts.given;

public class a_response_phase_database : Specification
{
    static readonly IServiceProvider _sharedServiceProvider;

    protected ResponsePhaseDbContext _context = null!;
    SqliteConnection _connection = null!;

    static a_response_phase_database()
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkSqlite()
            .AddSingleton<IModelCustomizer, ConceptAsModelCustomizer>()
            .AddSingleton<IEvaluatableExpressionFilter, ConceptAsEvaluatableExpressionFilter>()
            .AddSingleton<IInterceptor>(new ConceptAsQueryExpressionInterceptor())
            .AddSingleton<IInterceptor>(new ConceptAsDbCommandInterceptor());

        _sharedServiceProvider = services.BuildServiceProvider();
    }

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ResponsePhaseDbContext>()
            .UseSqlite(_connection)
            .UseInternalServiceProvider(_sharedServiceProvider)
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
