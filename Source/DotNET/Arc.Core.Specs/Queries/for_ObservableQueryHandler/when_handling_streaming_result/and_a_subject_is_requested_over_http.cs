// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler.when_handling_streaming_result;

public class and_a_subject_is_requested_over_http : for_ObservableQueryHttp.given.a_request
{
    QueryResult _written;
    int _status;

    async Task Because()
    {
        _request.WebSockets.Returns(Substitute.For<IWebSocketContext>());
        _request.Headers.Returns(new Dictionary<string, string>());
        _request.Query.Returns(new Dictionary<string, string>());
        _request.When(context => context.SetStatusCode(Arg.Any<int>())).Do(call => _status = call.Arg<int>());
        _request.WriteResponseAsJson(Arg.Any<object>(), typeof(QueryResult), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _written = (QueryResult)call.ArgAt<object>(0);
            return Task.CompletedTask;
        });
        var manager = Substitute.For<IQueryContextManager>();
        manager.Current.Returns(_queryContext);

        // The handler's root provider must never be used to resolve HTTP emission collaborators.
        var handler = new ObservableQueryHandler(manager, Substitute.For<IServiceProvider>(), Substitute.For<ILogger<ObservableQueryHandler>>());
        using var subject = new BehaviorSubject<string>("http");
        await handler.HandleStreamingResult(_request, "Observe", subject);
    }

    [Fact] void should_write_intercepted_data() => _written.Data.ShouldEqual("http-intercepted");
    [Fact] void should_write_ok() => _status.ShouldEqual((int)HttpStatusCode.OK);
    [Fact] void should_guard_with_request_services() => _guardContexts.Single().ServiceProvider.ShouldEqual(_services);
}
