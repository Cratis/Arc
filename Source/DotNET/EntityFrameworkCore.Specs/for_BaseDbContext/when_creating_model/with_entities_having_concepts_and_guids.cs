// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.for_BaseDbContext.when_creating_model;

public class with_entities_having_concepts_and_guids : given.a_base_db_context
{
    TestDbContext _dbContext;
    IModel _model;
    IEntityType _personEntityType;
    IEntityType _companyEntityType;

    void Establish()
    {
        _dbContext = CreateDbContext<TestDbContext>();
    }

    void Because()
    {
        _model = GetModel(_dbContext);
        _personEntityType = _model.FindEntityType(typeof(Person));
        _companyEntityType = _model.FindEntityType(typeof(Company));
    }

    [Fact] void should_apply_conversion_to_person_id_concept() => _personEntityType.FindProperty(nameof(Person.Id)).GetValueConverter().ShouldNotBeNull();
    [Fact] void should_apply_conversion_to_person_name_concept() => _personEntityType.FindProperty(nameof(Person.Name)).GetValueConverter().ShouldNotBeNull();
    [Fact] void should_apply_conversion_to_age_concept() => _personEntityType.FindProperty(nameof(Person.Age)).GetValueConverter().ShouldNotBeNull();
    [Fact] void should_apply_conversion_to_person_external_id_guid() => _personEntityType.FindProperty(nameof(Person.ExternalId)).GetValueConverter().ShouldNotBeNull();
    [Fact] void should_apply_conversion_to_company_id_guid() => _companyEntityType.FindProperty(nameof(Company.Id)).GetValueConverter().ShouldNotBeNull();
}
