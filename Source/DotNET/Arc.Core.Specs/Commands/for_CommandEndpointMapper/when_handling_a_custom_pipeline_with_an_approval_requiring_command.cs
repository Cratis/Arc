// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_handling_a_custom_pipeline_with_an_approval_requiring_command
{
    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public async Task should_refuse_execute_and_validate_without_entering_the_pipeline(bool registerFlow, bool authorized, bool supportedConfiguration)
    {
        var pipeline = Substitute.For<ICommandPipeline>();
        var flow = Substitute.For<IApprovalFlow>();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(ProtectedCommand));
        handler.Location.Returns(["Features", "Commands"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        var mapper = new a_recording_endpoint_mapper();
        await using var mappingServices = new ServiceCollection()
            .AddSingleton(handlers)
            .AddSingleton(Options.Create(new ArcOptions()))
            .BuildServiceProvider();
        mapper.MapCommandEndpoints(mappingServices);
        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.New());
        var authorization = Substitute.For<IAuthorizationEvaluator>();
        authorization.IsAuthorized(typeof(ProtectedCommand)).Returns(authorized);
        var registrations = new ServiceCollection()
            .AddLogging()
            .AddSingleton(pipeline)
            .AddSingleton(Options.Create(new ArcOptions()))
            .AddSingleton(correlationIdAccessor)
            .AddSingleton(authorization)
            .AddSingleton(new AuthorizationDeclarations(
                new KnownInstancesOf<IAnonymousEvaluator>([]),
                new KnownInstancesOf<IAuthorizationAttributeEvaluator>(supportedConfiguration ? [] : [new PolicyDeclaration()])));
        if (registerFlow) registrations.AddSingleton(flow);
        await using var services = registrations.BuildServiceProvider();
        foreach (var endpoint in mapper.Mapped)
        {
            var context = Substitute.For<IHttpRequestContext>();
            context.RequestServices.Returns(services);
            context.Headers.Returns(new Dictionary<string, string>());
            context.ReadBodyAsJson(typeof(ProtectedCommand), Arg.Any<CancellationToken>()).Returns(new ProtectedCommand());
            await endpoint.Handler(context);
            context.Received(1).SetStatusCode(403);
            var expectedReason = authorized && supportedConfiguration ? ApprovalGate.RefusalReason : string.Empty;
            await context.Received(1).WriteResponseAsJson(
                Arg.Is<object?>(value => value is CommandResult && !((CommandResult)value).IsAuthorized && !((CommandResult)value).HasExceptions && ((CommandResult)value).AuthorizationFailureReason == expectedReason),
                Arg.Any<Type>(),
                Arg.Any<CancellationToken>());
        }

        pipeline.ReceivedCalls().ShouldBeEmpty();
        flow.ReceivedCalls().ShouldBeEmpty();
    }

    [RequiresApproval]
    public record ProtectedCommand;

    class PolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(ProtectedCommand) ? [AuthorizationRequirement.FromAttribute(null, "Protected", null)] : [];
    }
}
