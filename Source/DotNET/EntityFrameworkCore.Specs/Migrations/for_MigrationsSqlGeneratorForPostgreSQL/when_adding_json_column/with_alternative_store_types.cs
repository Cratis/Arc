// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.when_adding_json_column;

public class with_alternative_store_types : given.a_migrations_sql_generator
{
    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("jsonb")]
    void should_index_a_jsonb_expression_without_changing_the_column_type(string storeType)
    {
        var commands = Generate("Documents", "Payload", storeType);
        commands[0].CommandText.ShouldContain($"ADD \"Payload\" {storeType} NOT NULL;");
        commands[2].CommandText.ShouldContain("USING GIN ((\"Payload\"::jsonb) jsonb_path_ops);");
    }
}
