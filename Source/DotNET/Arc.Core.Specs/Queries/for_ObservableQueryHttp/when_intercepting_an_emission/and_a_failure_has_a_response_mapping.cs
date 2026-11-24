// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Arc.Exceptions;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_a_failure_has_a_response_mapping : given.a_request
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "namespace_closing", true)]
    [InlineData(HttpStatusCode.Conflict, "namespace_recovery_required", false)]
    async Task should_map_before_committing_the_snapshot(HttpStatusCode status, string code, bool retryAfter)
    {
        var mapper = Substitute.For<IExceptionResponseMapper>();
        var headers = retryAfter ? new Dictionary<string, string> { ["Retry-After"] = "5" } : [];
        mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code, headers);
            return true;
        });
        await using var services = new ServiceCollection()
            .AddSingleton(_interceptors)
            .AddSingleton(_guards)
            .AddSingleton(_accessor)
            .AddSingleton(Options.Create(new ArcOptions { ExposeExceptionDetails = true }))
            .AddSingleton<IInstancesOf<IExceptionResponseMapper>>(new KnownInstancesOf<IExceptionResponseMapper>([mapper]))
            .BuildServiceProvider();
        _request.RequestServices.Returns(services);
        using var subject = new Subject<string>();
        var pending = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(2)), _request);
        subject.OnError(new InvalidOperationException("private failure detail"));
        _response = await pending;

        _response.StatusCode.ShouldEqual(status);
        _response.Result.ErrorCode.ShouldEqual(code);
        _response.Result.IsSuccess.ShouldBeFalse();
        _response.Result.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
        _response.Result.ExceptionStackTrace.ShouldBeEmpty();
        if (retryAfter) _request.Received(1).SetResponseHeader("Retry-After", "5");
        else _request.DidNotReceive().SetResponseHeader(Arg.Any<string>(), Arg.Any<string>());
    }
}
