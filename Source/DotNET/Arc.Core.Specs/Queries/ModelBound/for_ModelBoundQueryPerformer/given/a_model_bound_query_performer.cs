// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.given;

public class a_model_bound_query_performer : Specification
{
    protected ModelBoundQueryPerformer _performer;
    protected QueryContext _context;
    protected object? _result;
    protected IServiceProviderIsService _serviceProviderIsService;
    protected IAuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
    }

    protected void EstablishPerformer<T>(string methodName, object[]? dependencies = null, QueryArguments? parameters = null)
    {
        var method = typeof(T).GetMethod(methodName);
        _performer = new ModelBoundQueryPerformer(typeof(T), method!, _serviceProviderIsService, _authorizationEvaluator);

        parameters ??= new QueryArguments();
        dependencies ??= [];

        _context = new QueryContext(
            _performer.FullyQualifiedName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            parameters,
            dependencies);
    }

    protected async Task PerformQuery() => _result = await _performer.Perform(_context);
}
