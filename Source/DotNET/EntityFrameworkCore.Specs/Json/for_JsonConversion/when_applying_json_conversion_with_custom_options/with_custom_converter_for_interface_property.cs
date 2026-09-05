// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonConversion.when_applying_json_conversion_with_custom_options;

public class with_custom_converter_for_interface_property : Specification
{
    JsonConversionOptions _options;
    ModelBuilder _modelBuilder;
    ValueConverter _converter;
    IMyRequest _roundTrippedValue;

    void Establish()
    {
        _options = new JsonConversionOptions();
        _options.JsonSerializerOptions.Converters.Add(new MyRequestJsonConverter());

        _modelBuilder = new ModelBuilder();
        _modelBuilder.Entity<EntityWithInterfaceJsonProperty>();
    }

    void Because()
    {
        var entityTypeBuilder = _modelBuilder.Entity<EntityWithInterfaceJsonProperty>();
        entityTypeBuilder.ApplyJsonConversion(DatabaseType.Sqlite, _options);

        var property = entityTypeBuilder.Metadata.FindProperty(nameof(EntityWithInterfaceJsonProperty.Request));
        _converter = property.GetValueConverter()!;

        // Serialize then deserialize using the registered value converter (simulates EF Core round-trip)
        var serialized = (string)_converter.ConvertToProvider(new DoThing(42))!;
        _roundTrippedValue = (IMyRequest)_converter.ConvertFromProvider(serialized)!;
    }

    [Fact] void should_have_a_value_converter() => _converter.ShouldNotBeNull();
    [Fact] void should_deserialize_to_the_correct_type() => _roundTrippedValue.ShouldBeOfExactType<DoThing>();
    [Fact] void should_preserve_the_count_value() => _roundTrippedValue.Count.ShouldEqual(42);
}
