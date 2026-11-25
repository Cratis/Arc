// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Exceptions;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_a_mapped_failure : given.a_query_request
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "namespace_closing", true)]
    [InlineData(HttpStatusCode.Conflict, "namespace_recovery_required", false)]
    async Task should_write_the_safe_mapped_response(HttpStatusCode status, string code, bool delay)
    {
        var result = QueryResult.Error(CorrelationId.New(), new InvalidOperationException("private failure detail"));
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>()).Returns(result);
        var exceptionMapper = Substitute.For<IExceptionResponseMapper>();
        exceptionMapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code, delay ? new Dictionary<string, string> { ["Retry-After"] = "5" } : null);
            return true;
        });
        var original = _context.RequestServices;
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(call => call.Arg<Type>() == typeof(IInstancesOf<IExceptionResponseMapper>)
            ? new KnownInstancesOf<IExceptionResponseMapper>([exceptionMapper]) : original.GetService(call.Arg<Type>()));
        _context.RequestServices.Returns(services);
        _arcOptions.ExposeExceptionDetails = true;

        await _mapper.HandlerFor("GET")(_context);

        _statusCode.ShouldEqual((int)status);
        result.ErrorCode.ShouldEqual(code);
        result.IsSuccess.ShouldBeFalse();
        result.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
        result.ExceptionStackTrace.ShouldBeEmpty();
        await _context.Received(1).WriteResponseAsJson(result, typeof(QueryResult), Arg.Any<CancellationToken>());
        if (delay) _context.Received(1).SetResponseHeader("Retry-After", "5");
        else _context.DidNotReceive().SetResponseHeader("Retry-After", Arg.Any<string>());
    }
}
