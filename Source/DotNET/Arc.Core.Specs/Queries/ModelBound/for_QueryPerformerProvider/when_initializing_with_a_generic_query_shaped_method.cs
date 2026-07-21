// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

/// <summary>
/// A generic helper on a read model returns the same shape its real queries do, so return type alone cannot tell
/// them apart. Registering it would route an endpoint that can only ever fail, since a query is invoked with
/// arguments resolved from the request and there is nothing to close its type parameters with.
/// </summary>
public class when_initializing_with_a_generic_query_shaped_method : Specification
{
    QueryPerformerProvider _provider;
    ITypes _types;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.All.Returns([typeof(ReadModelWithGenericHelper)]);
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();

        _registry = Substitute.For<IQueryMetadataRegistry>();
        _registry.All.Returns(new Dictionary<string, Type>());
    }

    void Because() => _provider = new QueryPerformerProvider(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_only_register_the_non_generic_query() => _provider.Performers.Select(_ => _.Name.Value).ShouldContainOnly(nameof(ReadModelWithGenericHelper.Totals));
}
