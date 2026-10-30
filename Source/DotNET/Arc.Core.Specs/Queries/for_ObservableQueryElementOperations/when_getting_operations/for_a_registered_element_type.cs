// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryElementOperations.when_getting_operations;

public class for_a_registered_element_type : Specification
{
    record RegisteredElement(string Value);
    record OtherElement(string Value);

    ObservableQueryElementOperations _registered;
    ObservableQueryElementOperations _result;

    void Establish()
    {
        _registered = new ObservableQueryElementOperationsFor<OtherElement>();
        ObservableQueryElementOperations.Register(typeof(RegisteredElement), _registered);
    }

    void Because() => _result = ObservableQueryElementOperations.For(typeof(RegisteredElement));

    [Fact] void should_return_the_registered_operations() => _result.ShouldBeSame(_registered);
}
