// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_MissingQueryableSortKey;

public class when_creating : Specification
{
    MissingQueryableSortKey _exception;

    void Because() => _exception = new MissingQueryableSortKey(typeof(string), typeof(Guid));

    [Fact] void should_name_the_element_type() => _exception.Message.ShouldContain(typeof(string).ToString());
    [Fact] void should_name_the_key_type() => _exception.Message.ShouldContain(typeof(Guid).ToString());
}
