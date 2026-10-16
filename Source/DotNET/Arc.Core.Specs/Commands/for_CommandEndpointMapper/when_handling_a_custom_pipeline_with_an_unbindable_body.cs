// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_handling_a_custom_pipeline_with_an_unbindable_body : Specification
{
    [Theory]
    [InlineData(false, 403)]
    [InlineData(true, 400)]
    async Task should_authorize_before_reporting_binding_failure(bool authorized, int expectedStatus)
    {
        var pipeline = Substitute.For<ICommandPipeline>();
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(SomeCommand)).Returns(authorized);
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(SomeCommand));
        handler.Location.Returns(["Commands"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        var accessor = Substitute.For<ICorrelationIdAccessor>();
        accessor.Current.Returns(CorrelationId.New());
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(pipeline).AddSingleton(evaluator).AddSingleton(handlers).AddSingleton(accessor)
            .AddSingleton(Options.Create(new ArcOptions()))
            .AddSingleton(new AuthorizationDeclarations(new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .BuildServiceProvider();
        var mapper = new a_recording_endpoint_mapper();
        mapper.MapCommandEndpoints(services);
        mapper.Mapped.Count.ShouldEqual(2);
        foreach (var endpoint in mapper.Mapped)
        {
            var context = Substitute.For<IHttpRequestContext>();
            context.RequestServices.Returns(services);
            context.Headers.Returns(new Dictionary<string, string>());
            context.ReadBodyAsJson(typeof(SomeCommand), Arg.Any<CancellationToken>()).Returns<Task<object?>>(_ => throw new JsonException("private", "$.Amount", null, null));
            await endpoint.Handler(context);
            context.Received(1).SetStatusCode(expectedStatus);
            if (authorized)
            {
                await context.Received(1).WriteResponseAsJson(Arg.Is<CommandResult>(result => result.ValidationResults.Single().Members.Single() == "amount"), typeof(CommandResult), Arg.Any<CancellationToken>());
            }
        }

        pipeline.ReceivedCalls().ShouldBeEmpty();
    }
}
