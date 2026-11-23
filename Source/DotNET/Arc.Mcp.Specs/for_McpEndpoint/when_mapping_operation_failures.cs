// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Exceptions;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_mapping_operation_failures : an_mcp_host
{
    IExceptionResponseMapper _mapper;

    protected override void ConfigureArcOptions(ArcOptions options) => options.ExposeExceptionDetails = true;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp
        .Expose<MappedFailureCommand>("fail_command", "Fails a command.")
        .ExposeQuery<MappedFailureReadModel>(nameof(MappedFailureReadModel.All), "fail_query", "Fails a query.");

    protected override void ConfigureServices(IServiceCollection services)
    {
        _mapper = Substitute.For<IExceptionResponseMapper>();
        services.AddSingleton<IInstancesOf<IExceptionResponseMapper>>(new KnownInstancesOf<IExceptionResponseMapper>([_mapper]));
        var handlers = new[] { typeof(WriteNote), typeof(MappedFailureCommand) }
            .Select(type => (ICommandHandler)new ModelBoundCommandHandler(type, type.GetMethod("Handle")!)).ToArray();
        var providers = Substitute.For<ICommandHandlerProviders>();
        providers.Handlers.Returns(handlers);
        providers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var found = handlers.SingleOrDefault(handler => handler.CommandType == call[0]!.GetType());
            call[1] = found;
            return found is not null;
        });
        services.AddSingleton(providers);
    }

    [Theory]
    [InlineData("fail_command", HttpStatusCode.ServiceUnavailable, "namespace_closing")]
    [InlineData("fail_query", HttpStatusCode.ServiceUnavailable, "namespace_closing")]
    [InlineData("fail_command", HttpStatusCode.Conflict, "namespace_recovery_required")]
    [InlineData("fail_query", HttpStatusCode.Conflict, "namespace_recovery_required")]
    async Task should_project_only_safe_metadata(string tool, HttpStatusCode status, string code)
    {
        _mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code);
            return true;
        });
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        var response = await CallTool(tool, "{}");
        response.Status.ShouldEqual(HttpStatusCode.OK);
        response.IsToolError.ShouldBeTrue();
        using var document = JsonDocument.Parse(response.ToolText);
        var outcome = document.RootElement.GetProperty("outcome");
        outcome.GetProperty("errorCode").GetString().ShouldEqual(code);
        outcome.GetProperty("isSuccess").GetBoolean().ShouldBeFalse();
        outcome.GetProperty("isRetryable").GetBoolean().ShouldBeFalse();
        response.ToolText.ShouldNotContain("private failure detail");
        outcome.TryGetProperty("exceptionMessages", out _).ShouldBeFalse();
        outcome.TryGetProperty("exceptionStackTrace", out _).ShouldBeFalse();
        response.Message.GetProperty("result").GetProperty("structuredContent").GetProperty("outcome").GetProperty("errorCode").GetString().ShouldEqual(code);
    }
}
