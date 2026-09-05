// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_Paging.when_computing_skip;

public class and_paging_is_within_range : Specification
{
    Paging _paging;
    int _result;

    void Establish() => _paging = new Paging(3, 10, true);

    void Because() => _result = _paging.Skip;

    [Fact] void should_skip_page_times_size() => _result.ShouldEqual(30);
}
