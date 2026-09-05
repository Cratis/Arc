// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_Paging.when_computing_skip;

public class and_the_page_is_negative : Specification
{
    Paging _paging;
    int _result;

    void Establish() => _paging = new Paging(-1, 10, true);

    void Because() => _result = _paging.Skip;

    [Fact] void should_clamp_to_zero() => _result.ShouldEqual(0);
}
