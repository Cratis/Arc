// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider.given;

public class a_query_performer_provider_with_no_metadata : Specification
{
    protected QueryPerformerProvider _provider;
    protected ITypes _types;
    protected IQueryMetadataRegistry _registry;
    protected IServiceProviderIsService _serviceProviderIsService;
    protected IAuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();

        _registry = Substitute.For<IQueryMetadataRegistry>();
        _registry.All.Returns(new Dictionary<string, Type>());
    }
}
