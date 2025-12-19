// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Arc.EntityFrameworkCore.for_AddColumnExtensions.given;

public class a_migration_builder : Specification
{
    protected MigrationBuilder CreateMigrationBuilder(string provider) => new(provider);

    protected MigrationBuilder CreateSqliteMigrationBuilder() => CreateMigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");

    protected MigrationBuilder CreateSqlServerMigrationBuilder() => CreateMigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

    protected MigrationBuilder CreatePostgreSqlMigrationBuilder() => CreateMigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
}
