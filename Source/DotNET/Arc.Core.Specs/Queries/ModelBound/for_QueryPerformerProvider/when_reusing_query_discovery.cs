// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_reusing_query_discovery : Specification
{
    ITypes _sharedUniverse;
    ITypes _otherUniverse;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;
    QueryPerformerProvider _first;
    QueryPerformerProvider _second;
    QueryPerformerProvider _other;
    QueryPerformerProvider _updated;

    void Establish()
    {
        _sharedUniverse = Substitute.For<ITypes>();
        _sharedUniverse.All.Returns([typeof(PublicReadModelWithValidQuery)]);
        _otherUniverse = Substitute.For<ITypes>();
        _otherUniverse.All.Returns([]);
        _registry = Substitute.For<IQueryMetadataRegistry>();
        _registry.All.Returns(new Dictionary<string, Type>());
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
    }

    void Because()
    {
        _first = Create(_sharedUniverse);
        _second = Create(_sharedUniverse);
        _other = Create(_otherUniverse);
        _otherUniverse.All.Returns([typeof(PublicReadModelWithValidQuery)]);
        _updated = Create(_otherUniverse);
    }

    [Fact] void should_check_the_shared_universe_for_changes() => _ = _sharedUniverse.Received(2).All;
    [Fact] void should_check_the_other_universe_for_changes() => _ = _otherUniverse.Received(2).All;
    [Fact] void should_keep_performers_in_both_containers() => _first.Performers.Count().ShouldEqual(_second.Performers.Count());
    [Fact] void should_not_reuse_performers_for_another_universe() => _other.Performers.ShouldBeEmpty();
    [Fact] void should_rediscover_when_the_same_universe_changes() => _updated.Performers.Count().ShouldEqual(1);
    [Fact] void should_create_container_owned_performers() => _first.Performers.Single().ShouldNotBeSame(_second.Performers.Single());

    QueryPerformerProvider Create(ITypes types) => new(types, _registry, _serviceProviderIsService, _authorizationEvaluator);
}
