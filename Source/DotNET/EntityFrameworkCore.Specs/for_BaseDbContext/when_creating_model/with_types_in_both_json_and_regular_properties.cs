// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.for_BaseDbContext.when_creating_model;

public class with_types_in_both_json_and_regular_properties : given.a_base_db_context
{
    TestDbContext _dbContext;
    IModel _model;
    IEntityType _organizationEntityType;
    IProperty _locationIdProperty;
    IProperty _referenceIdProperty;
    IProperty _metadataProperty;

    void Establish()
    {
        _dbContext = CreateDbContext<TestDbContext>();
    }

    void Because()
    {
        _model = GetModel(_dbContext);
        _organizationEntityType = _model.FindEntityType(typeof(Organization));
        _locationIdProperty = _organizationEntityType.FindProperty(nameof(Organization.LocationId));
        _referenceIdProperty = _organizationEntityType.FindProperty(nameof(Organization.ReferenceId));
        _metadataProperty = _organizationEntityType.FindProperty(nameof(Organization.Metadata));
    }

    [Fact] void should_apply_concept_conversion_to_location_id() => _locationIdProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_apply_guid_conversion_to_reference_id() => _referenceIdProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_apply_json_conversion_to_metadata() => _metadataProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_serialize_metadata_as_json() => _metadataProperty.GetValueConverter().ProviderClrType.ShouldEqual(typeof(string));
    [Fact] void should_apply_conversion_to_organization_id_guid() => _organizationEntityType.FindProperty(nameof(Organization.Id)).GetValueConverter().ShouldNotBeNull();
}
