// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authorization;
using Cratis.Arc.Exceptions;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_a_custom_pipeline_returns_a_mapped_failure : Specification
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "namespace_closing", true)]
    [InlineData(HttpStatusCode.Conflict, "namespace_recovery_required", false)]
    async Task should_map_execute_and_validate_responses(HttpStatusCode status, string code, bool delay)
    {
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>()).Returns(_ => Failure());
        pipeline.Validate(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>()).Returns(_ => Failure());
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(SomeCommand));
        handler.Location.Returns(["Features", "Commands"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        var exceptionMapper = Substitute.For<IExceptionResponseMapper>();
        exceptionMapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code, delay ? new Dictionary<string, string> { ["Retry-After"] = "5" } : null);
            return true;
        });
        var correlation = Substitute.For<ICorrelationIdAccessor>();
        correlation.Current.Returns(CorrelationId.New());
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(handlers)
            .AddSingleton(pipeline)
            .AddSingleton(correlation)
            .AddSingleton(Options.Create(new ArcOptions { ExposeExceptionDetails = true }))
            .AddSingleton<IInstancesOf<IExceptionResponseMapper>>(new KnownInstancesOf<IExceptionResponseMapper>([exceptionMapper]))
            .AddSingleton(new AuthorizationDeclarations(new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .BuildServiceProvider();
        var mapper = new a_recording_endpoint_mapper();
        mapper.MapCommandEndpoints(services);
        foreach (var endpoint in mapper.Mapped)
        {
            var context = Substitute.For<IHttpRequestContext>();
            context.RequestServices.Returns(services);
            context.Headers.Returns(new Dictionary<string, string>());
            context.ReadBodyAsJson(typeof(SomeCommand), Arg.Any<CancellationToken>()).Returns(new SomeCommand(0, string.Empty));
            CommandResult written = null!;
            context.WriteResponseAsJson(Arg.Any<object?>(), Arg.Any<Type>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                written = (CommandResult)call[0]!;
                return Task.CompletedTask;
            });
            await endpoint.Handler(context);
            context.Received(1).SetStatusCode((int)status);
            written.ErrorCode.ShouldEqual(code);
            written.IsSuccess.ShouldBeFalse();
            written.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
            written.ExceptionStackTrace.ShouldBeEmpty();
            if (delay) context.Received(1).SetResponseHeader("Retry-After", "5");
            else context.DidNotReceive().SetResponseHeader("Retry-After", Arg.Any<string>());
        }
    }

    static CommandResult Failure() => CommandResult.FromException(CorrelationId.New(), new InvalidOperationException("private failure detail"));
}
