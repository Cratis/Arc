// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.for_BaseDbContext.when_creating_model;

public class with_types_only_in_json_properties : given.a_base_db_context
{
    TestDbContext _dbContext;
    IModel _model;
    IEntityType _departmentEntityType;
    IProperty _settingsProperty;

    void Establish()
    {
        _dbContext = CreateDbContext<TestDbContext>();
    }

    void Because()
    {
        _model = GetModel(_dbContext);
        _departmentEntityType = _model.FindEntityType(typeof(Department));
        _settingsProperty = _departmentEntityType.FindProperty(nameof(Department.Settings));
    }

    [Fact]
    void should_not_have_settings_id_concept_in_department_properties()
    {
        var properties = _departmentEntityType.GetProperties();
        var settingsIdProperty = properties.FirstOrDefault(p => p.Name == "SettingsId");
        settingsIdProperty.ShouldBeNull();
    }

    [Fact]
    void should_not_have_tracking_id_guid_in_department_properties()
    {
        var properties = _departmentEntityType.GetProperties();
        var trackingIdProperty = properties.FirstOrDefault(p => p.Name == "TrackingId");
        trackingIdProperty.ShouldBeNull();
    }

    [Fact] void should_apply_json_conversion_to_settings_property() => _settingsProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_serialize_settings_as_json() => _settingsProperty.GetValueConverter().ProviderClrType.ShouldEqual(typeof(string));
    [Fact] void should_apply_conversion_to_department_id_guid() => _departmentEntityType.FindProperty(nameof(Department.Id)).GetValueConverter().ShouldNotBeNull();
}
