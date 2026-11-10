// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryRowNarrower;

public class when_no_narrowing_applies : Specification
{
    object _data;
    object _result;

    async Task Because()
    {
        _data = new List<object> { "a" };
        _result = await QueryRowNarrower.Narrow("Q", typeof(string), _data, QueryContext.NotSet, new ServiceCollection().BuildServiceProvider());
    }

    [Fact] void should_return_the_result_as_it_is() => _result.ShouldBeSame(_data);
}
