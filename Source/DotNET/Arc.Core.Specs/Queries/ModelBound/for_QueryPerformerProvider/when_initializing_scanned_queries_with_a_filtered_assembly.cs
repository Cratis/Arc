// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_initializing_scanned_queries_with_a_filtered_assembly : given.a_provider_with_an_assembly_filter
{
    void Because() => _provider = CreateProvider(_excluded);

    [Fact] void should_not_have_performers() => _provider.Performers.ShouldBeEmpty();
    [Fact] void should_not_find_the_query() => _provider.TryGetPerformerFor(_queryName, out _).ShouldBeFalse();
}
