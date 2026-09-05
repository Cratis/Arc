// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_TypeReferenceConverter.when_converting;

/// <summary>
/// A concept keeps its own name rather than being reduced to the primitive behind it, because the concept is
/// declared once at the top of the document and referenced by name from there on.
/// </summary>
public class a_reference_with_modifiers : given.a_type_reference_converter
{
    TypeRefSyntax _concept;
    TypeRefSyntax _optional;
    TypeRefSyntax _collection;
    TypeRefSyntax _unnamed;

    void Because()
    {
        _concept = _converter.Convert(new TypeReferenceModel("AuthorId", false, false));
        _optional = _converter.Convert(new TypeReferenceModel("Int", false, true));
        _collection = _converter.Convert(new TypeReferenceModel("AuthorName", true, false));
        _unnamed = _converter.Convert(new TypeReferenceModel(string.Empty, false, false));
    }

    [Fact] void should_name_a_concept_after_the_concept() => _concept.Name.ShouldEqual("AuthorId");
    [Fact] void should_not_mark_a_plain_reference_as_optional() => _concept.IsOptional.ShouldBeFalse();
    [Fact] void should_not_mark_a_plain_reference_as_a_collection() => _concept.IsCollection.ShouldBeFalse();
    [Fact] void should_keep_a_primitive_name() => _optional.Name.ShouldEqual("Int");
    [Fact] void should_mark_an_optional_reference_as_optional() => _optional.IsOptional.ShouldBeTrue();
    [Fact] void should_keep_the_element_name_of_a_collection() => _collection.Name.ShouldEqual("AuthorName");
    [Fact] void should_mark_a_collection_as_a_collection() => _collection.IsCollection.ShouldBeTrue();
    [Fact] void should_fall_back_for_a_reference_with_no_name() => _unnamed.Name.ShouldEqual("String");
}
