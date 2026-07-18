// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_Paging.when_computing_skip;

public class and_the_product_overflows : Specification
{
    Paging _paging;
    int _result;

    void Establish() => _paging = new Paging(999999999, int.MaxValue, true);

    void Because() => _result = _paging.Skip;

    [Fact] void should_clamp_to_the_maximum() => _result.ShouldEqual(int.MaxValue);
    [Fact] void should_not_be_negative() => (_result >= 0).ShouldBeTrue();
}
