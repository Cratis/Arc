// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_scanning_selected_queries : given.a_query_performer_provider_with_no_metadata
{
    void Establish() => _types.All.Returns([typeof(SelectedReadModel), typeof(NoQueries), typeof(ConventionalReadModel), typeof(QueryVisibilityReadModel)]);
    void Because() => _provider = new(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);
    [Fact] void should_discover_selected_and_conventional_queries() => _provider.Performers.Count().ShouldEqual(6);
    [Fact] void should_expose_only_the_public_query_on_a_conventional_model() => _provider.Performers.Where(performer => performer.Type == typeof(QueryVisibilityReadModel)).Select(performer => performer.Name.Value).ShouldContainOnly("Public");
    [Fact] void should_ignore_unmarked_helpers() => _provider.Performers.Any(_ => _.Name == "Reduce").ShouldBeFalse();
    [Fact] void should_preserve_selected_authorization() => _provider.Performers.Single(_ => _.Name == "ById").AllowsAnonymousAccess.ShouldBeTrue();
    [Fact] void should_preserve_selected_routes() => _provider.Performers.Single(_ => _.Name == "ById").CustomRoute.ShouldEqual("/api/selection/by-id");
    [Fact] void should_select_the_marked_overload() => _provider.Performers.Single(_ => _.Name == "ById").Parameters.Single().Type.ShouldEqual(typeof(string));
}
