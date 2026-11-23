// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Exceptions;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_the_subject_has_a_mapped_failure : given.a_guarded_client_observable
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "namespace_closing")]
    [InlineData(HttpStatusCode.Conflict, "namespace_recovery_required")]
    async Task should_send_the_safe_failure_without_changing_http_metadata(HttpStatusCode status, string code)
    {
        var mapper = Substitute.For<IExceptionResponseMapper>();
        mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code, new Dictionary<string, string> { ["Retry-After"] = "5" });
            return true;
        });
        _context.RequestServices.GetService(typeof(IInstancesOf<IExceptionResponseMapper>)).Returns(new KnownInstancesOf<IExceptionResponseMapper>([mapper]));
        var received = new TaskCompletionSource<QueryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _webSocketConnectionHandler.SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            received.TrySetResult(call.Arg<QueryResult>());
            return Task.FromResult<Exception?>(null);
        });
        await RunConnection(async () =>
        {
            _subject.OnError(new InvalidOperationException("private failure detail"));
            await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        });
        var result = await received.Task;
        result.ErrorCode.ShouldEqual(code);
        result.IsSuccess.ShouldBeFalse();
        result.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
        result.ExceptionStackTrace.ShouldBeEmpty();
        _context.DidNotReceive().SetStatusCode((int)status);
        _context.DidNotReceive().SetResponseHeader("Retry-After", Arg.Any<string>());
    }
}
