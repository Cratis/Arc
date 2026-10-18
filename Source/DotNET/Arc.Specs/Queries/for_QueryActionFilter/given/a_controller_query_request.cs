// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.Execution;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryActionFilter.given;

public class a_controller_query_request : a_query_action_filter
{
    protected ActionExecutionDelegate _next;
    protected ActionExecutedContext _executed;
    protected IHttpWebSocketFeature _webSocketFeature;
    protected WebSocket _socket;
    protected List<string> _frames;
    protected List<string> _transportEvents;
    protected CorrelationId _correlationId;
    protected ArcOptions _options;
    protected IWebSocketConnectionHandler _connectionHandler;
    ServiceProvider _services;
    MemoryStream _body;
    System.Diagnostics.ActivitySource _source;

    void Establish()
    {
        _options = new ArcOptions();
        _correlationId = CorrelationId.New();
        _connectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _services = new ServiceCollection().AddLogging().AddSingleton(Options.Create(_options))
            .AddSingleton(_readModelInterceptors).AddSingleton(_connectionHandler)
            .AddSingleton(Substitute.For<IHttpRequestContextAccessor>())
            .AddSingleton(Substitute.For<Microsoft.Extensions.Hosting.IHostApplicationLifetime>())
            .AddSingleton(Substitute.For<IObservableQueryEmissionGuards>()).BuildServiceProvider();
        _httpContext.RequestServices = _services;
        _httpContext.Request.Headers[Constants.DefaultCorrelationIdHeader] = _correlationId.ToString();
        _body = new MemoryStream();
        _httpContext.Response.Body = _body;
        _queryContextManager.Current.Returns(new QueryContext("Controller.Observe", _correlationId, Paging.NotPaged, Sorting.None));
        _controllerAdapter = new ControllerObservableQueryAdapter(_queryContextManager, Substitute.For<ILogger<ControllerObservableQueryAdapter>>());
        _filter = new QueryActionFilter(_queryContextManager, _queryRenderers, _readModelInterceptors, _controllerAdapter, _logger, CreateActivitySource());
        _executed = new ActionExecutedContext(_actionContext, [], null!);
        _next = Substitute.For<ActionExecutionDelegate>();
        _next().Returns(Task.FromResult(_executed));
        _frames = [];
        _transportEvents = [];
        _socket = Substitute.For<WebSocket>();
        _socket.SendAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _frames.Add(System.Text.Encoding.UTF8.GetString(call.Arg<ArraySegment<byte>>()));
                _transportEvents.Add("send");
                return Task.CompletedTask;
            });
        _socket.CloseAsync(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _transportEvents.Add("close");
                return Task.CompletedTask;
            });
        _webSocketFeature = Substitute.For<IHttpWebSocketFeature>();
        _webSocketFeature.AcceptAsync(Arg.Any<Microsoft.AspNetCore.Http.WebSocketAcceptContext>())
            .Returns(_ =>
            {
                _transportEvents.Add("accept");
                return Task.FromResult(_socket);
            });
        _httpContext.Features.Set(_webSocketFeature);
    }

    void Destroy()
    {
        _source.Dispose();
        _body.Dispose();
        _socket.Dispose();
        _services.Dispose();
    }

    protected void InvalidArguments() => _actionContext.ModelState.AddModelError("MinimumBalance", "Must be non-negative");

    protected QueryResult Deserialize(string json) => JsonSerializer.Deserialize<QueryResult>(json, _options.JsonSerializerOptions)!;

    protected string ResponseBody() => System.Text.Encoding.UTF8.GetString(_body.ToArray());

    Cratis.Traces.IActivitySource<QueryActionFilter> CreateActivitySource()
    {
        var source = Substitute.For<Cratis.Traces.IActivitySource<QueryActionFilter>>();
        _source = new System.Diagnostics.ActivitySource("ControllerQueryValidation");
        source.ActualSource.Returns(_source);
        return source;
    }
}
