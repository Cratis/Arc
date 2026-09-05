// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonConversion.when_applying_json_conversion_with_custom_options;

public class when_no_custom_options_are_supplied : Specification
{
    ModelBuilder _modelBuilder;
    ValueConverter _converter;
    PersonName _roundTrippedValue;

    void Establish()
    {
        _modelBuilder = new ModelBuilder();
        _modelBuilder.Entity<EntityWithJsonProperties>();
    }

    void Because()
    {
        var entityTypeBuilder = _modelBuilder.Entity<EntityWithJsonProperties>();
        entityTypeBuilder.ApplyJsonConversion(DatabaseType.Sqlite);

        var property = entityTypeBuilder.Metadata.FindProperty(nameof(EntityWithJsonProperties.Name));
        _converter = property.GetValueConverter()!;

        var serialized = (string)_converter.ConvertToProvider(new PersonName("John", "Doe"))!;
        _roundTrippedValue = (PersonName)_converter.ConvertFromProvider(serialized)!;
    }

    [Fact] void should_have_a_value_converter() => _converter.ShouldNotBeNull();
    [Fact] void should_round_trip_first_name() => _roundTrippedValue.FirstName.ShouldEqual("John");
    [Fact] void should_round_trip_last_name() => _roundTrippedValue.LastName.ShouldEqual("Doe");
}
