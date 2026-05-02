// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.for_BaseDbContext.when_creating_model;

public class with_json_properties : given.a_base_db_context
{
    TestDbContext _dbContext;
    IModel _model;
    IEntityType _companyEntityType;
    IProperty _metadataProperty;

    void Establish()
    {
        _dbContext = CreateDbContext<TestDbContext>();
    }

    void Because()
    {
        _model = GetModel(_dbContext);
        _companyEntityType = _model.FindEntityType(typeof(Company));
        _metadataProperty = _companyEntityType.FindProperty(nameof(Company.Metadata));
    }

    [Fact] void should_apply_conversion_to_company_id_guid() => _companyEntityType.FindProperty(nameof(Company.Id)).GetValueConverter().ShouldNotBeNull();
    [Fact] void should_apply_json_conversion_to_metadata_property() => _metadataProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_serialize_metadata_as_json() => _metadataProperty.GetValueConverter().ProviderClrType.ShouldEqual(typeof(string));
}
