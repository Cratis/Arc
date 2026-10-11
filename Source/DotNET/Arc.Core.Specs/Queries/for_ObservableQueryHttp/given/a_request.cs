// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.given;

public class a_request : Specification
{
    protected QueryContext _queryContext;
    protected IHttpRequestContext _request;
    protected IReadModelInterceptors _interceptors;
    protected IObservableQueryEmissionGuards _guards;
    protected IHttpRequestContextAccessor _accessor;
    protected ServiceProvider _services;
    protected ObservableQueryHttpResponse _response;
    protected TaskCompletionSource _guardEntered;
    protected List<ObservableQueryEmissionContext> _guardContexts;
    protected ArcOptions _arcOptions;
    protected ILogger _logger;

    void Establish()
    {
        _queryContext = new("Application.Observe", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _guardEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _guardContexts = [];
        _interceptors = Substitute.For<IReadModelInterceptors>();
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<object>>().Select(value => value is string text ? $"{text}-intercepted" : value)));
        _guards = Substitute.For<IObservableQueryEmissionGuards>();
        _guards.HasGuards.Returns(true);
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(call =>
        {
            _guardContexts.Add(call.Arg<ObservableQueryEmissionContext>());
            _guardEntered.TrySetResult();
            return Task.FromResult(ObservableQueryEmissionVerdict.Allow);
        });
        _accessor = new HttpRequestContextAccessor();
        _arcOptions = new ArcOptions { ExposeExceptionDetails = false };
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(LogLevel.Error).Returns(true);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);
        _services = new ServiceCollection()
            .AddSingleton(Options.Create(_arcOptions))
            .AddSingleton(loggerFactory)
            .AddSingleton(_interceptors)
            .AddSingleton(_guards)
            .AddSingleton(_accessor)
            .BuildServiceProvider();
        _request = Substitute.For<IHttpRequestContext>();
        _request.RequestServices.Returns(_services);
        _request.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "subscriber")]));
        _request.RequestAborted.Returns(CancellationToken.None);
    }

    void Destroy() => _services.Dispose();
}
