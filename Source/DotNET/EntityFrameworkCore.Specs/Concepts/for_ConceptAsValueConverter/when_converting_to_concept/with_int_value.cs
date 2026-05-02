// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_ConceptAsValueConverter.when_converting_to_concept;

public class with_int_value : given.a_concept_as_value_converter
{
    int _value;
    TestIntConcept _result;

    void Establish()
    {
        _value = 42;
    }

    void Because() => _result = (TestIntConcept)_intConverter.ConvertFromProvider(_value);

    [Fact] void should_create_concept_with_the_value() => _result.Value.ShouldEqual(42);
}
