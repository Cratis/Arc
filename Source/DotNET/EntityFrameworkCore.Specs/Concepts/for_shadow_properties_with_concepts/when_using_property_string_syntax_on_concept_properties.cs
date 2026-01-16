// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_shadow_properties_with_concepts;

public class when_using_property_string_syntax_on_concept_properties : given.a_test_database
{
    IProperty _sampleIdProperty;
    IProperty _relatedIdProperty;

    void Because()
    {
        var entityType = _context.Model.FindEntityType(typeof(EntityWithConceptPropertiesConfiguredAsShadow));
        _sampleIdProperty = entityType.FindProperty("SampleId");
        _relatedIdProperty = entityType.FindProperty("RelatedId");
    }

    [Fact] void should_create_context_without_errors() => _context.ShouldNotBeNull();
    [Fact] void should_have_configured_sample_id_property() => _sampleIdProperty.ShouldNotBeNull();
    [Fact] void should_have_correct_clr_type_for_sample_id() => _sampleIdProperty.ClrType.ShouldEqual(typeof(SampleId));
    [Fact] void should_have_configured_related_id_property() => _relatedIdProperty.ShouldNotBeNull();
    [Fact] void should_have_correct_clr_type_for_related_id() => _relatedIdProperty.ClrType.ShouldEqual(typeof(RelatedId));
    [Fact] void should_have_value_converter_for_sample_id() => _sampleIdProperty.GetValueConverter().ShouldNotBeNull();
}
