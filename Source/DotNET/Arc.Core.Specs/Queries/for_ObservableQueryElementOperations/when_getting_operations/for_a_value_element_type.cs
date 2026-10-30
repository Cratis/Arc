// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryElementOperations.when_getting_operations;

public class for_a_value_element_type : Specification
{
    record struct Element(int Value);

    ObservableQueryElementOperations _first;
    ObservableQueryElementOperations _second;

    void Because()
    {
        _first = ObservableQueryElementOperations.For(typeof(Element));
        _second = ObservableQueryElementOperations.For(typeof(Element));
    }

    [Fact] void should_create_operations_closed_over_the_element_type() => _first.ShouldBeOfExactType<ObservableQueryElementOperationsFor<Element>>();
    [Fact] void should_cache_the_operations() => _second.ShouldBeSame(_first);
}
