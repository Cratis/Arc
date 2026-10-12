// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_generated_metadata_selects_queries : given.a_query_performer_provider_with_no_metadata
{
    void Establish()
    {
        _types.All.Returns([typeof(SelectedReadModel), typeof(NoQueries), typeof(ConventionalReadModel)]);
        _registry.All.Returns(new Dictionary<string, Type>
        {
            [$"{typeof(SelectedReadModel).FullName}.ById"] = typeof(SelectedReadModel),
            [$"{typeof(SelectedReadModel).FullName}.Observe"] = typeof(SelectedReadModel),
            [$"{typeof(SelectedReadModel).FullName}.Create"] = typeof(SelectedReadModel)
        });
    }

    void Because() => _provider = new(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_select_the_marked_overload() => _provider.Performers.Single(_ => _.Name == "ById").Parameters.Single().Type.ShouldEqual(typeof(string));
    [Fact] void should_ignore_a_stale_unmarked_registry_entry() => _provider.Performers.Count(_ => _.Name == "Create").ShouldEqual(1);
    [Fact] void should_not_hide_unregistered_owners_in_the_same_assembly() => _provider.Performers.Count().ShouldEqual(5);
}
