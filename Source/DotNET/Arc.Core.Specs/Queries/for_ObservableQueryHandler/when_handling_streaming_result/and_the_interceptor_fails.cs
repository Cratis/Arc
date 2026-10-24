// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler.when_handling_streaming_result;

public class and_the_interceptor_fails : for_ObservableQueryHttp.given.a_request
{
    const string SecretMessage = "Confidential interceptor failure detail";
    QueryResult _written;
    int _status;

    void Establish()
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
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    async Task should_log_the_failure_and_honor_exception_detail_exposure(bool exposeExceptionDetails, bool delayed)
    {
        _arcOptions.ExposeExceptionDetails = exposeExceptionDetails;
        var failure = new InterceptionFailed(SecretMessage);
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns<Task<IEnumerable<object>>>(_ => throw failure);
        var manager = Substitute.For<IQueryContextManager>();
        manager.Current.Returns(_queryContext);
        var handler = new ObservableQueryHandler(manager, _services, Substitute.For<ILogger<ObservableQueryHandler>>());
        using var delayedSubject = new Subject<string>();
        using var cachedSubject = new BehaviorSubject<string>("private");
        ISubject<string> subject = delayed ? delayedSubject : cachedSubject;
        if (delayed)
        {
            _request.Query.Returns(new Dictionary<string, string> { [ObservableQueryHttp.WaitForFirstResultQueryStringKey] = "true" });
        }
        var response = handler.HandleStreamingResult(_request, "Observe", subject);
        if (delayed)
        {
            subject.OnNext("private");
        }
        await response;

        _status.ShouldEqual(500);
        _written.Data.ShouldBeNull();
        _written.ExceptionMessages.ShouldContainOnly(exposeExceptionDetails ? SecretMessage : ExceptionDetailRedactor.RedactedMessage);
        _written.ExceptionStackTrace.ShouldEqual(exposeExceptionDetails ? failure.StackTrace : string.Empty);
        failure.StackTrace.ShouldNotBeEmpty();
        JsonSerializer.Serialize(_written).Contains(SecretMessage, StringComparison.Ordinal).ShouldEqual(exposeExceptionDetails);
        _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[3] == failure).ShouldBeTrue();
    }

    class InterceptionFailed(string message) : Exception(message);
}
