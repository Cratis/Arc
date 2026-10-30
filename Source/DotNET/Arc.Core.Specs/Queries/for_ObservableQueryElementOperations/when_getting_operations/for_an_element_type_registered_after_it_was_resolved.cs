// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryElementOperations.when_getting_operations;

public class for_an_element_type_registered_after_it_was_resolved : Specification
{
    record Element(string Value);
    record OtherElement(string Value);

    ObservableQueryElementOperations _resolved;
    ObservableQueryElementOperations _result;

    void Establish() => _resolved = ObservableQueryElementOperations.For(typeof(Element));

    void Because()
    {
        ObservableQueryElementOperations.Register(typeof(Element), new ObservableQueryElementOperationsFor<OtherElement>());
        _result = ObservableQueryElementOperations.For(typeof(Element));
    }

    [Fact] void should_keep_the_first_operations() => _result.ShouldBeSame(_resolved);
}
