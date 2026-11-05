// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Approvals.for_ApprovalGate;

[Collection("UsesCurrentDirectory")]
public class when_an_approval_requiring_command_is_called_over_http
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_require_approval_without_invoking_the_handler(bool registerFlow)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        var probe = new Probe();
        builder.Services.AddSingleton(probe);
        if (registerFlow) builder.Services.AddSingleton<IApprovalFlow>(new ApprovalFlow(probe));
        RegisterArtifacts(builder.Services);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            using var plain = await client.PostAsJsonAsync("/approval-spec/plain", new { }, global::Xunit.TestContext.Current.CancellationToken);
            plain.StatusCode.ShouldEqual(HttpStatusCode.OK);
            probe.Commands.ShouldEqual(1);

            using var execution = await client.PostAsJsonAsync("/approval-spec/marked", new { }, global::Xunit.TestContext.Current.CancellationToken);
            await AssertApprovalResponse(execution, registerFlow);
            probe.Commands.ShouldEqual(1);
            probe.Flows.ShouldEqual(registerFlow ? 1 : 0);

            using var validation = await client.PostAsJsonAsync("/approval-spec/marked/validate", new { }, global::Xunit.TestContext.Current.CancellationToken);
            await AssertApprovalResponse(validation, registerFlow);
            probe.Commands.ShouldEqual(1);
            probe.Flows.ShouldEqual(registerFlow ? 1 : 0);
        }
        finally
        {
            await StopHost(app);
        }
    }

    static async Task AssertApprovalResponse(HttpResponseMessage response, bool registerFlow)
    {
        response.StatusCode.ShouldEqual(registerFlow ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
        if (!registerFlow)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(global::Xunit.TestContext.Current.CancellationToken));
            body.RootElement.GetProperty("isAuthorized").GetBoolean().ShouldBeFalse();
            body.RootElement.GetProperty("authorizationFailureReason").GetString().ShouldEqual(ApprovalGate.RefusalReason);
        }
    }

    static async Task StopHost(WebApplication app)
    {
        // Finish host teardown even when test-runner cancellation has fired.
#pragma warning disable xUnit1051
        await app.StopAsync();
#pragma warning restore xUnit1051
    }

    static void RegisterArtifacts(IServiceCollection services)
    {
        var handlers = new[] { typeof(MarkedCommand), typeof(PlainCommand) }
            .Select(type => (ICommandHandler)new ModelBoundCommandHandler(type, type.GetMethod("Handle")!)).ToArray();
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns(handlers);
        commands.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var found = handlers.SingleOrDefault(handler => handler.CommandType == call[0]!.GetType());
            call[1] = found;
            return found is not null;
        });
        services.AddSingleton(commands);
        services.AddSingleton(Substitute.For<ICommandKeys>());
        var queries = Substitute.For<IQueryPerformerProviders>();
        queries.Performers.Returns([]);
        services.AddSingleton(queries);
    }

    [Path("/approval-spec/marked"), RequiresApproval]
    public record MarkedCommand { public void Handle(Probe probe) => probe.Commands++; }

    [Path("/approval-spec/plain")]
    public record PlainCommand { public void Handle(Probe probe) => probe.Commands++; }

    public class Probe
    {
        public int Commands { get; set; }
        public int Flows { get; set; }
    }

    sealed class ApprovalFlow(Probe probe) : IApprovalFlow
    {
        public Task<CommandResult> Begin(CommandContext context)
        {
            probe.Flows++;
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }
    }
}
