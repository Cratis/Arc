// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_AvailabilityFilter;

public class when_no_gate_is_registered : Specification
{
    QueryResult _result;

    async Task Because()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var context = new QueryContext("Any.Query", CorrelationId.New(), Paging.NotPaged, Sorting.None, null, null, services);
        _result = await new AvailabilityFilter(Substitute.For<IQueryPerformerProviders>()).OnPerform(context);
    }

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
