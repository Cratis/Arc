// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Migrations.for_MigrationsSqlGeneratorForPostgreSQL;

[Collection(PostgreSqlCollection.Name)]
public class when_applying_add_json_column(PostgreSqlFixture fixture) : Specification
{
    given.JsonColumnDbContext _context;

    void Establish()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"json_migration_{Guid.NewGuid():N}"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<given.JsonColumnDbContext>()
            .UseNpgsql(connectionString)
            .ReplaceService<IMigrationsSqlGenerator, MigrationsSqlGeneratorForPostgreSQL>()
            .Options;
        _context = new given.JsonColumnDbContext(options);
    }

    Task Because() => _context.Database.MigrateAsync();

    async Task Destroy()
    {
        try
        {
            await _context.Database.EnsureDeletedAsync();
        }
        finally
        {
            await _context.DisposeAsync();
        }
    }

    [Fact]
    async Task should_create_the_jsonb_column() =>
        (await _context.Database.SqlQueryRaw<string>("SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_schema = 'JsonDocuments' AND table_name = 'Observers' AND column_name = 'TypeSetControl'").SingleAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldEqual("jsonb");

    [Fact]
    async Task should_create_the_check_constraint() =>
        (await _context.Database.SqlQueryRaw<string>("SELECT pg_get_constraintdef(c.oid) AS \"Value\" FROM pg_constraint c JOIN pg_class t ON t.oid = c.conrelid JOIN pg_namespace n ON n.oid = t.relnamespace WHERE n.nspname = 'JsonDocuments' AND t.relname = 'Observers' AND c.conname = 'CK_Observers_TypeSetControl_IsJson' AND c.contype = 'c'").SingleAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldContain("jsonb_typeof");

    [Fact]
    async Task should_create_the_gin_index() =>
        (await _context.Database.SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'JsonDocuments' AND tablename = 'Observers' AND indexname = 'IX_Observers_TypeSetControl_Jsonb'").SingleAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldContain("USING gin");

    [Fact]
    async Task should_use_the_jsonb_path_operator_class() =>
        (await _context.Database.SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'JsonDocuments' AND tablename = 'Observers' AND indexname = 'IX_Observers_TypeSetControl_Jsonb'").SingleAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldContain("jsonb_path_ops");

    [Fact]
    async Task should_preserve_an_explicit_text_column_type() =>
        (await _context.Database.SqlQueryRaw<string>("SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_schema = 'JsonDocuments' AND table_name = 'Observers' AND column_name = 'TextPayload'").SingleAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldEqual("text");

    [Fact]
    async Task should_index_an_explicit_text_column_as_jsonb() =>
        (await _context.Database.SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'JsonDocuments' AND tablename = 'Observers' AND indexname = 'IX_Observers_TextPayload_Jsonb'").SingleAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldContain("::jsonb");
}
