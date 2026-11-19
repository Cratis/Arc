// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Validation;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryPipeline.given;

public class a_paged_query_with_a_disposed_global_provider : a_query_pipeline
{
    protected Paging _paging;
    protected QueryResult _result;
    protected object _queryData;
    IServiceProvider _originalGlobalProvider;
    ServiceProvider _requestProvider;

    void Establish()
    {
        _paging = new Paging(1, 2, true);
        _queryData = new[] { new { name = "third" } };
        _requestProvider = new ServiceCollection()
            .AddSingleton(new AuthorizationDeclarations(
                new KnownInstancesOf<IAnonymousEvaluator>([]),
                new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .AddTransient<PageNumberValidator>()
            .AddTransient<PageSizeValidator>()
            .BuildServiceProvider();
        _serviceProvider = _requestProvider;

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(PageNumberValidator), typeof(PageSizeValidator)]);
        _discoverableValidators = new DiscoverableValidators(types);
        var activitySource = Substitute.For<IActivitySource<QueryPipeline>>();
        activitySource.ActualSource.Returns(_activitySource);
        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            query_filters,
            _queryPerformerProviders,
            _queryRenderers,
            _readModelInterceptors,
            _discoverableValidators,
            activitySource);

        _queryPerformerProviders.TryGetPerformersFor("SomeQuery", out Arg.Any<IQueryPerformer>()).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(_queryData));
        _queryRenderers.Render("SomeQuery", _queryData, _serviceProvider).Returns(new QueryRendererResult(3, _queryData));

        try
        {
            _originalGlobalProvider = Internals.ServiceProvider;
        }
        catch (ServiceProviderNotConfigured)
        {
            _originalGlobalProvider = null!;
        }

        var disposedProvider = new ServiceCollection().BuildServiceProvider();
        disposedProvider.Dispose();
        Internals.ServiceProvider = disposedProvider;
    }

    void Destroy()
    {
        Internals.ServiceProvider = _originalGlobalProvider;
        _requestProvider.Dispose();
    }
}
