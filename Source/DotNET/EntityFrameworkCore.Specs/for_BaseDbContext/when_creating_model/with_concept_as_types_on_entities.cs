// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.for_BaseDbContext.when_creating_model;

public class with_concept_as_types_on_entities : given.a_base_db_context
{
    TestDbContext _dbContext;
    IModel _model;

    void Establish()
    {
        _dbContext = CreateDbContext<TestDbContext>();
    }

    void Because()
    {
        _model = GetModel(_dbContext);
    }

    [Fact] void should_not_have_person_id_as_entity_type() => _model.FindEntityType(typeof(PersonId)).ShouldBeNull();
    [Fact] void should_not_have_person_name_as_entity_type() => _model.FindEntityType(typeof(PersonName)).ShouldBeNull();
    [Fact] void should_not_have_age_as_entity_type() => _model.FindEntityType(typeof(Age)).ShouldBeNull();
    [Fact] void should_not_have_metadata_id_as_entity_type() => _model.FindEntityType(typeof(MetadataId)).ShouldBeNull();
    [Fact] void should_not_have_settings_id_as_entity_type() => _model.FindEntityType(typeof(SettingsId)).ShouldBeNull();
    [Fact] void should_not_have_location_id_as_entity_type() => _model.FindEntityType(typeof(LocationId)).ShouldBeNull();
    [Fact] void should_have_person_as_entity_type() => _model.FindEntityType(typeof(Person)).ShouldNotBeNull();
    [Fact] void should_have_company_as_entity_type() => _model.FindEntityType(typeof(Company)).ShouldNotBeNull();
}
