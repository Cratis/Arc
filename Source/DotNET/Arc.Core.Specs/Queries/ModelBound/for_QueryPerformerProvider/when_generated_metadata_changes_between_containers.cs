// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_generated_metadata_changes_between_containers : Specification
{
    ITypes _types;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;
    Dictionary<string, Type> _metadata;
    QueryPerformerProvider _first;
    QueryPerformerProvider _second;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _registry = Substitute.For<IQueryMetadataRegistry>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        _metadata = new Dictionary<string, Type>
        {
            [$"First.Model.{nameof(PublicReadModelWithValidQuery.GetById)}"] = typeof(PublicReadModelWithValidQuery)
        };
        _registry.All.Returns(_metadata);
    }

    void Because()
    {
        _first = Create();
        _metadata[$"Second.Model.{nameof(PublicReadModelWithInternalQuery.Query)}"] = typeof(PublicReadModelWithInternalQuery);
        _second = Create();
    }

    [Fact] void should_not_change_the_first_containers_performers() => _first.Performers.Count().ShouldEqual(1);
    [Fact] void should_include_new_metadata_in_the_next_container() => _second.Performers.Count().ShouldEqual(2);
    [Fact] void should_not_scan_reflection_types() => _ = _types.DidNotReceive().All;

    QueryPerformerProvider Create() => new(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);
}
