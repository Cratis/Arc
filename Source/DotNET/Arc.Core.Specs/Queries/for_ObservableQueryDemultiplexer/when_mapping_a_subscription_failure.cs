// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Arc.Exceptions;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_mapping_a_subscription_failure : given.an_observable_query_demultiplexer
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "namespace_closing")]
    [InlineData(HttpStatusCode.Conflict, "namespace_recovery_required")]
    async Task should_deliver_a_safe_failure_envelope_without_changing_the_transport(HttpStatusCode status, string code)
    {
        var mapper = Substitute.For<IExceptionResponseMapper>();
        mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code, new Dictionary<string, string> { ["Retry-After"] = "5" });
            return true;
        });
        await using var services = new ServiceCollection()
            .AddSingleton(_fallbackAuthorization)
            .AddSingleton(_performers)
            .AddSingleton<IInstancesOf<IExceptionResponseMapper>>(new KnownInstancesOf<IExceptionResponseMapper>([mapper]))
            .BuildServiceProvider();
        var hub = new ObservableQueryDemultiplexer(_queryPipeline, _queryContextManager, _httpRequestContextAccessor, _hostApplicationLifetime, _readModelInterceptors, services, _arcOptions, Substitute.For<IQueryHealthTracker>(), _emissionGuards, _logger);
        using var subject = new Subject<string>();
        var received = new TaskCompletionSource<QueryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var legacyErrors = 0;
        var context = Substitute.For<IHttpRequestContext>();
        var subscriptionContext = new ObservableQuerySubscriptionContext(
            context,
            "query-1",
            PagingInfo.NotPaged,
            null,
            CorrelationId.New(),
            new ObservableQuerySubscriptionIdentity("[TestApp].[TestQuery]", QueryArguments.Empty, null),
            (result, _) =>
            {
                received.TrySetResult(result);
                return Task.CompletedTask;
            },
            (_, _, _) =>
            {
                legacyErrors++;
                return Task.CompletedTask;
            },
            (_, _) => Task.CompletedTask,
            () => completed.TrySetResult(),
            null,
            TestContext.Current.CancellationToken);
        using var subscription = hub.SubscribeToStreamingData(subscriptionContext, subject);
        subject.OnError(new InvalidOperationException("private failure detail"));
        var failure = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        failure.ErrorCode.ShouldEqual(code);
        failure.IsSuccess.ShouldBeFalse();
        failure.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
        failure.ExceptionStackTrace.ShouldBeEmpty();
        legacyErrors.ShouldEqual(0);
        context.DidNotReceive().SetStatusCode(Arg.Any<int>());
        context.DidNotReceive().SetResponseHeader(Arg.Any<string>(), Arg.Any<string>());
    }
}
