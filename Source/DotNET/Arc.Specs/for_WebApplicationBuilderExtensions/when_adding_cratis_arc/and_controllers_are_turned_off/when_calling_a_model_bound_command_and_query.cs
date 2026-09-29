// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc.and_controllers_are_turned_off;

[Collection("UsesCurrentDirectory")]
public class when_calling_a_model_bound_command_and_query : Specification
{
    WebApplication? _app;
    HttpClient? _client;
    HttpResponseMessage _commandResponse;
    HttpResponseMessage _queryResponse;
    JsonElement _queryData;

    async Task Establish()
    {
        RegisterGreeting.Received = null;

        var commandHandler = new ModelBoundCommandHandler(typeof(RegisterGreeting), typeof(RegisterGreeting).GetMethod(nameof(RegisterGreeting.Handle))!);
        var commandHandlers = Substitute.For<ICommandHandlerProviders>();
        commandHandlers.Handlers.Returns([commandHandler]);
        commandHandlers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var matches = call[0] is RegisterGreeting;
            call[1] = matches ? commandHandler : null;
            return matches;
        });

        await using var classification = new ServiceCollection().BuildServiceProvider();
        var authorization = Substitute.For<IAuthorizationEvaluator>();
        authorization.IsAuthorized(Arg.Any<Type>()).Returns(true);
        authorization.IsAuthorized(Arg.Any<System.Reflection.MethodInfo>()).Returns(true);
        var queryPerformer = new ModelBoundQueryPerformer(
            typeof(Greeting),
            typeof(Greeting).FullName!,
            typeof(Greeting).GetMethod(nameof(Greeting.All))!,
            classification.GetRequiredService<IServiceProviderIsService>(),
            authorization);
        var queryPerformers = Substitute.For<IQueryPerformerProviders>();
        queryPerformers.Performers.Returns([queryPerformer]);
        queryPerformers.TryGetPerformersFor(queryPerformer.FullyQualifiedName, out Arg.Any<IQueryPerformer?>()).Returns(call =>
        {
            call[1] = queryPerformer;
            return true;
        });

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        builder.Services.AddSingleton(commandHandlers);
        builder.Services.AddSingleton(queryPerformers);
        _app = builder.Build();
        _app.UseCratisArc();
        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    async Task Because()
    {
        _commandResponse = await _client!.PostAsJsonAsync(RouteFor($"Execute{typeof(RegisterGreeting).FullName}"), new { text = "Hello" });

        var queryEndpoint = Endpoints().First(_ => _.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName.EndsWith(typeof(Greeting).FullName + "." + nameof(Greeting.All), StringComparison.Ordinal) == true);
        _queryResponse = await _client.GetAsync("/" + queryEndpoint.RoutePattern.RawText!.TrimStart('/'));
        using var document = JsonDocument.Parse(await _queryResponse.Content.ReadAsStringAsync());
        _queryData = document.RootElement.GetProperty("data").Clone();
    }

    async Task Destroy()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact] void should_execute_the_command() => _commandResponse.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_hand_the_command_to_its_handler() => RegisterGreeting.Received.ShouldEqual("Hello");
    [Fact] void should_perform_the_query() => _queryResponse.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_return_the_query_result() => _queryData[0].GetProperty("text").GetString().ShouldEqual("Hi");

    IEnumerable<RouteEndpoint> Endpoints() =>
        ((IEndpointRouteBuilder)_app!).DataSources.SelectMany(_ => _.Endpoints).OfType<RouteEndpoint>();

    string RouteFor(string name) =>
        "/" + Endpoints().Single(_ => _.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name).RoutePattern.RawText!.TrimStart('/');

    public record RegisterGreeting(string Text)
    {
        public static string? Received { get; set; }

        public void Handle() => Received = Text;
    }

    public record Greeting(string Text)
    {
        public static IEnumerable<Greeting> All() => [new("Hi")];
    }
}
