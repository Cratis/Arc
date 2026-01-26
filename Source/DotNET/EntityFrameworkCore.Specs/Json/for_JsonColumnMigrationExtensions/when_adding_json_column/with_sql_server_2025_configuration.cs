// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonColumnMigrationExtensions.when_adding_json_column;

public class with_sql_server_2025_configuration : Specification
{
    MigrationBuilder _mb;
    AddColumnOperation _result;

    void Establish()
    {
        SqlServerVersionDetector.AssumeServer2025();
        _mb = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
    }

    void Because()
    {
        _mb.AddJsonColumn<object>("Data", "TestTable");
        _result = (AddColumnOperation)_mb.Operations[0];
    }

    void Cleanup() => SqlServerVersionDetector.Reset();

    [Fact] void should_set_type_to_json() => _result.ColumnType.ShouldEqual("json");
    [Fact] void should_mark_as_json() => _result.IsJson().ShouldBeTrue();
    [Fact] void should_not_be_nullable() => _result.IsNullable.ShouldBeFalse();
}
