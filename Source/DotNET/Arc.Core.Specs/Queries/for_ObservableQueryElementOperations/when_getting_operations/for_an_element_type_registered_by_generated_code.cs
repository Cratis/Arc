// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryElementOperations.when_getting_operations;

public class for_an_element_type_registered_by_generated_code : Specification
{
    record GeneratedElement(string Value);

    ObservableQueryElementOperations _first;
    ObservableQueryElementOperations _second;

    void Establish()
    {
        ObservableQueryElementTypes.Register<GeneratedElement>();
        ObservableQueryElementTypes.Register<GeneratedElement>();
    }

    void Because()
    {
        _first = ObservableQueryElementOperations.For(typeof(GeneratedElement));
        _second = ObservableQueryElementOperations.For(typeof(GeneratedElement));
    }

    [Fact] void should_return_operations_closed_over_the_element_type() => _first.ShouldBeOfExactType<ObservableQueryElementOperationsFor<GeneratedElement>>();
    [Fact] void should_return_the_same_operations_every_time() => _second.ShouldBeSame(_first);
}
