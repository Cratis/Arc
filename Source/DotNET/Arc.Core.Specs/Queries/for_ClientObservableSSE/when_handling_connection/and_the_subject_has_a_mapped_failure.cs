// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Exceptions;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_the_subject_has_a_mapped_failure : given.a_guarded_client_observable_sse
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
        await RunConnection(async () =>
        {
            _subject.OnError(new InvalidOperationException("private failure detail"));
            await WaitFor(() => WrittenResults.Any());
        });
        var result = WrittenResults.Single();
        var message = _messages.Single(value => value.StartsWith("data: ", StringComparison.Ordinal));
        using var payload = JsonDocument.Parse(message["data: ".Length..].Trim());
        payload.RootElement.GetProperty("errorCode").GetString().ShouldEqual(code);
        result.IsSuccess.ShouldBeFalse();
        result.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
        result.ExceptionStackTrace.ShouldBeEmpty();
        _context.DidNotReceive().SetStatusCode((int)status);
        _context.DidNotReceive().SetResponseHeader("Retry-After", Arg.Any<string>());
    }
}
