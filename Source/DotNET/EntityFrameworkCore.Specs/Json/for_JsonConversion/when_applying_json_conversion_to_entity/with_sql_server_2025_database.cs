// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonConversion.when_applying_json_conversion_to_entity;

public class with_sql_server_2025_database : given.a_json_conversion_context
{
    ModelBuilder _modelBuilder;
    IMutableProperty _nameProperty;
    IMutableProperty _addressProperty;

    void Establish()
    {
        SqlServerVersionDetector.AssumeServer2025();
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);
        _modelBuilder = new ModelBuilder();
        _modelBuilder.Entity<EntityWithJsonProperties>();
    }

    void Because()
    {
        var entityTypeBuilder = _modelBuilder.Entity<EntityWithJsonProperties>();
        entityTypeBuilder.ApplyJsonConversion(DatabaseType.SqlServer2025);
        _nameProperty = entityTypeBuilder.Metadata.FindProperty(nameof(EntityWithJsonProperties.Name))!;
        _addressProperty = entityTypeBuilder.Metadata.FindProperty(nameof(EntityWithJsonProperties.Address))!;
    }

    void Cleanup() => SqlServerVersionDetector.Reset();

    [Fact] void should_set_name_column_type_to_json() => _nameProperty.GetColumnType().ShouldEqual("json");
    [Fact] void should_set_address_column_type_to_json() => _addressProperty.GetColumnType().ShouldEqual("json");
    [Fact] void should_have_value_converter_on_name_property() => _nameProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_have_value_converter_on_address_property() => _addressProperty.GetValueConverter().ShouldNotBeNull();
}
