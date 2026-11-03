// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_initializing_generated_queries_with_an_included_assembly : given.a_generated_provider_with_an_assembly_filter
{
    void Because() => _provider = CreateProvider(_included);

    [Fact] void should_have_a_performer() => _provider.Performers.Count().ShouldEqual(1);
    [Fact] void should_find_the_generated_query() => _provider.TryGetPerformerFor(_queryName, out _).ShouldBeTrue();
    [Fact] void should_preserve_the_generated_query_name() => _provider.Performers.Single().FullyQualifiedName.Value.ShouldEqual(_queryName);
}
