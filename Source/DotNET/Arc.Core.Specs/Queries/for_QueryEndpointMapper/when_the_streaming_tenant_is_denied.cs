// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Tenancy;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper;

public class when_the_streaming_tenant_is_denied : given.a_query_request
{
    void Establish()
    {
        var result = QueryResult.Success(CorrelationId.New());
        result.Data = new object();
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
        _observableQueryHandler.IsStreamingResult(result.Data).Returns(true);
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns(_ => throw new TargetInvocationException(new TenantMembershipDenied()));
        var original = _context.RequestServices;
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(call => call.Arg<Type>() == typeof(TenantIdAccessor) ? new TenantIdAccessor(resolver) : original.GetService(call.Arg<Type>()));
        _context.RequestServices.Returns(services);
    }
    async Task Because()
    {
        using var boundary = TenantIdAccessor.Independent.BeginRequest();
        await _mapper.HandlerFor("GET")(_context);
    }
    [Fact] void should_respond_forbidden() => _statusCode.ShouldEqual(403);
    [Fact] void should_return_an_unauthorized_result() => _context.Received(1).WriteResponseAsJson(Arg.Is<QueryResult>(result => !result.IsAuthorized && !result.HasExceptions), typeof(QueryResult), Arg.Any<CancellationToken>());
    [Fact] void should_not_commit_streaming_headers() => _context.DidNotReceive().SetResponseHeader("Cache-Control", "no-cache");
}
