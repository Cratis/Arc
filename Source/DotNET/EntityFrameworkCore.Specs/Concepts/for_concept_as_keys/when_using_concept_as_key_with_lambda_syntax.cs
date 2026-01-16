// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_concept_as_keys;

public class when_using_concept_as_key_with_lambda_syntax : given.a_test_database
{
    IEntityType _entityType;
    IProperty _idProperty;
    IKey _key;

    void Because()
    {
        _entityType = _context.Model.FindEntityType(typeof(EntityWithConceptKey))!;
        _idProperty = _entityType.FindProperty(nameof(EntityWithConceptKey.Id))!;
        _key = _entityType.FindPrimaryKey()!;
    }

    [Fact] void should_have_entity_type_configured() => _entityType.ShouldNotBeNull();
    [Fact] void should_have_all_properties() => _entityType.GetProperties().ShouldNotBeEmpty();
    [Fact] void should_have_key_configured() => _key.ShouldNotBeNull();
    [Fact] void should_have_concept_property() => _idProperty.ShouldNotBeNull();
    [Fact] void should_have_value_converter_on_property() => _idProperty.GetValueConverter().ShouldNotBeNull();
    [Fact] void should_not_treat_key_as_shadow_property() => _idProperty.IsShadowProperty().ShouldBeFalse();
}
