// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_a_query_is_overloaded_by_a_fold_creator : given.a_query_performer_provider_with_no_metadata
{
    void Establish()
    {
        _types.All.Returns([typeof(OverloadedReadModel), typeof(FoldOnlyReadModel)]);
        _registry.All.Returns(new Dictionary<string, Type>
        {
            [$"{typeof(OverloadedReadModel).FullName}.Create"] = typeof(OverloadedReadModel)
        });
    }

    void Because() => _provider = new(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_discover_only_the_real_query() => _provider.Performers.Count().ShouldEqual(1);
    [Fact] void should_select_the_string_overload() => _provider.Performers.Single().Parameters.Single().Type.ShouldEqual(typeof(string));
}
