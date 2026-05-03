// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.for_BaseDbContext.when_creating_model;

public class with_property_types_only_on_json_entities : given.a_base_db_context
{
    TestDbContext _dbContext;
    IModel _model;
    IEntityType _storeEntityType;
    IProperty _configurationProperty;

    void Establish()
    {
        _dbContext = CreateDbContext<TestDbContext>();
    }

    void Because()
    {
        _model = GetModel(_dbContext);
        _storeEntityType = _model.FindEntityType(typeof(Store));
        _configurationProperty = _storeEntityType.FindProperty(nameof(Store.Configuration));
    }

    [Fact]
    void should_not_have_configuration_details_as_separate_entity()
    {
        var configurationDetailsEntity = _model.FindEntityType(typeof(ConfigurationDetails));
        configurationDetailsEntity.ShouldBeNull();
    }

    [Fact]
    void should_not_have_configuration_code_property_in_store()
    {
        var properties = _storeEntityType.GetProperties();
        var configurationCodeProperty = properties.FirstOrDefault(p => p.Name == "ConfigurationCode");
        configurationCodeProperty.ShouldBeNull();
    }

    [Fact]
    void should_not_have_unique_id_property_in_store()
    {
        var properties = _storeEntityType.GetProperties();
        var uniqueIdProperty = properties.FirstOrDefault(p => p.Name == "UniqueId");
        uniqueIdProperty.ShouldBeNull();
    }

    [Fact] void should_apply_json_conversion_to_configuration_property() => _configurationProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_serialize_configuration_as_json() => _configurationProperty.GetValueConverter().ProviderClrType.ShouldEqual(typeof(string));
    [Fact] void should_apply_conversion_to_store_id_guid() => _storeEntityType.FindProperty(nameof(Store.Id)).GetValueConverter().ShouldNotBeNull();
}
