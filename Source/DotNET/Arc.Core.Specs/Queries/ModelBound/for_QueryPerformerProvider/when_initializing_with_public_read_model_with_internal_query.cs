// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_initializing_with_public_read_model_with_internal_query : Specification
{
    QueryPerformerProvider _provider;
    ITypes _types;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.All.Returns([typeof(PublicReadModelWithInternalQuery)]);
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
    }

    void Because() => _provider = new QueryPerformerProvider(_types, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_have_one_performer() => _provider.Performers.Count().ShouldEqual(1);
}
