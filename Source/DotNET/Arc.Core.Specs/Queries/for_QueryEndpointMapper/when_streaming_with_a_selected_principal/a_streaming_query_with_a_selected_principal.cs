// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_streaming_with_a_selected_principal;

/// <summary>
/// A direct streaming query whose pipeline selected another principal. The scopes it hands out are observable, so a
/// spec can tell which provider the streamed context resolves through and when it is disposed.
/// </summary>
public abstract class a_streaming_query_with_a_selected_principal : given.a_query_request
{
    protected IServiceProvider _requestServices;
    protected IServiceScope _ownedScope;
    protected IServiceProvider _providerSeenByHandler;
    protected bool _ownedScopeDisposedDuringHandler;
    protected bool _useOwnedScope = true;
    protected Func<Task> _handler = () => Task.CompletedTask;
    Exception _failure;

    protected Exception Failure => _failure;

    void Establish()
    {
        _requestServices = _context.RequestServices;
        var ownedProvider = new ServiceCollection().BuildServiceProvider();
        _ownedScope = Substitute.For<IServiceScope>();
        _ownedScope.ServiceProvider.Returns(ownedProvider);

        var manager = Substitute.For<IQueryContextManager>();
        manager.Current.Returns(QueryContext.NotSet);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(call =>
        {
            var type = call.Arg<Type>();
            if (type == typeof(IQueryContextManager))
            {
                return manager;
            }

            return type == typeof(TenantIdAccessor) ? null : _requestServices.GetService(type);
        });
        _context.RequestServices.Returns(services);

        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var result = QueryResult.Success(CorrelationId.New());
                result.Data = new object();
                result.AuthorizedTenant = "tenant";
                result.AuthorizedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "bob")], "test"));
                result.OwnedScope = _useOwnedScope ? _ownedScope : null;
                return Task.FromResult(result);
            });
        _observableQueryHandler.IsStreamingResult(Arg.Any<object?>()).Returns(true);
        _observableQueryHandler.HandleStreamingResult(Arg.Any<IHttpRequestContext>(), Arg.Any<QueryName>(), Arg.Any<object>())
            .Returns(async call =>
            {
                _providerSeenByHandler = call.Arg<IHttpRequestContext>().RequestServices;
                _ownedScopeDisposedDuringHandler = _ownedScope.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(IDisposable.Dispose));
                await _handler();
            });
    }

    protected async Task Run()
    {
        using var boundary = TenantIdAccessor.Independent.BeginRequest();
        _failure = await Catch.Exception(() => _mapper.HandlerFor("GET")(_context));
    }

    protected bool OwnedScopeWasDisposed() => _ownedScope.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IDisposable.Dispose)) == 1;
}
