// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_ConceptAsValueConverter.when_converting_from_concept;

public class with_string_concept : given.a_concept_as_value_converter
{
    TestStringConcept _concept;
    string _result;

    void Establish()
    {
        _concept = new TestStringConcept("test value");
    }

    void Because() => _result = (string)_stringConverter.ConvertToProvider(_concept);

    [Fact] void should_extract_the_underlying_string_value() => _result.ShouldEqual("test value");
}
