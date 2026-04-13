// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_initializing_with_generated_metadata : Specification
{
    QueryPerformerProvider _provider;
    ITypes _types;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();

        _registry = Substitute.For<IQueryMetadataRegistry>();
        _registry.All.Returns(
        [
            new StubQueryMetadata(
                new Dictionary<string, Type>
                {
                    [$"{typeof(PublicReadModelWithValidQuery).FullName}.{nameof(PublicReadModelWithValidQuery.GetById)}"] = typeof(PublicReadModelWithValidQuery)
                })
        ]);
    }

    void Because() => _provider = new QueryPerformerProvider(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_have_one_performer() => _provider.Performers.Count().ShouldEqual(1);
    [Fact] void should_not_use_reflection_types() => _ = _types.DidNotReceive().All;
}
