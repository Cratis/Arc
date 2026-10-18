// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.given;

[DbContext(typeof(JsonColumnDbContext))]
[Migration("20261017000000_AddJsonColumns")]
public class AddJsonColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("JsonDocuments");
        migrationBuilder.CreateTable(
            name: "Observers",
            columns: columns => new { Id = columns.Column<int>(nullable: false) },
            schema: "JsonDocuments",
            constraints: table => table.PrimaryKey("PK_Observers", entity => entity.Id));
        migrationBuilder.AddJsonColumn<string>("TypeSetControl", "Observers", "JsonDocuments");
        migrationBuilder.AddJsonColumn<string>("TextPayload", "Observers", "JsonDocuments");
        ((AddColumnOperation)migrationBuilder.Operations[^1]).ColumnType = "text";
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Observers", "JsonDocuments");
}
