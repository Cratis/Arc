// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_enumerable_of_primitive_or_concept;

public class with_additional_query_argument_types : Specification
{
    bool _dates;
    bool _times;
    bool _uris;
    bool _nullableIntegers;
    bool _nullableDates;

    void Because()
    {
        _dates = typeof(IEnumerable<DateOnly>).IsEnumerableOfPrimitiveOrConcept();
        _times = typeof(IEnumerable<TimeOnly>).IsEnumerableOfPrimitiveOrConcept();
        _uris = typeof(IEnumerable<Uri>).IsEnumerableOfPrimitiveOrConcept();
        _nullableIntegers = typeof(IEnumerable<int?>).IsEnumerableOfPrimitiveOrConcept();
        _nullableDates = typeof(IEnumerable<DateOnly?>).IsEnumerableOfPrimitiveOrConcept();
    }

    [Fact] void should_classify_dates() => _dates.ShouldBeTrue();
    [Fact] void should_classify_times() => _times.ShouldBeTrue();
    [Fact] void should_classify_uris() => _uris.ShouldBeTrue();
    [Fact] void should_classify_nullable_integers() => _nullableIntegers.ShouldBeTrue();
    [Fact] void should_classify_nullable_dates() => _nullableDates.ShouldBeTrue();
}
