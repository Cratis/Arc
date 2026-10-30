// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_MissingObservableQueryElementType;

public class when_creating : Specification
{
    MissingObservableQueryElementType _exception;

    void Because() => _exception = new MissingObservableQueryElementType(typeof(string));

    [Fact] void should_name_the_element_type() => _exception.Message.ShouldContain(typeof(string).ToString());
}
