// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc.and_controllers_are_turned_off;

[Collection("UsesCurrentDirectory")]
public class when_calling_a_command_that_fails_validation : Specification
{
    WebApplication? _app;
    HttpClient? _client;
    HttpResponseMessage _response;
    CommandResult? _result;

    async Task Establish()
    {
        RegisterInvalidGreeting.Received = null;

        var commandHandler = new ModelBoundCommandHandler(typeof(RegisterInvalidGreeting), typeof(RegisterInvalidGreeting).GetMethod(nameof(RegisterInvalidGreeting.Handle))!);
        var commandHandlers = Substitute.For<ICommandHandlerProviders>();
        commandHandlers.Handlers.Returns([commandHandler]);
        commandHandlers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var matches = call[0] is RegisterInvalidGreeting;
            call[1] = matches ? commandHandler : null;
            return matches;
        });

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
        builder.Services.AddSingleton(commandHandlers);
        _app = builder.Build();
        _app.UseCratisArc();
        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    async Task Because()
    {
        var route = "/" + ((IEndpointRouteBuilder)_app!).DataSources
            .SelectMany(_ => _.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(_ => _.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == $"Execute{typeof(RegisterInvalidGreeting).FullName}")
            .RoutePattern.RawText!.TrimStart('/');

        _response = await _client!.PostAsJsonAsync(route, new { text = "Hello" });
        _result = await _response.Content.ReadFromJsonAsync<CommandResult>(_app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ArcOptions>>().Value.JsonSerializerOptions);
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

    [Fact] void should_respond_with_bad_request() => _response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_return_a_validation_failed_result() => _result!.IsValid.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _result!.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_validation_message() => _result!.ValidationResults.Single().Message.ShouldEqual("The text is not accepted");
    [Fact] void should_not_hand_the_command_to_its_handler() => RegisterInvalidGreeting.Received.ShouldBeNull();

    public record RegisterInvalidGreeting(string Text)
    {
        public static string? Received { get; set; }

        public void Handle() => Received = Text;
    }

    public class RegisterInvalidGreetingValidator : CommandValidator<RegisterInvalidGreeting>
    {
        public RegisterInvalidGreetingValidator() =>
            RuleFor(_ => _.Text).Must(_ => false).WithMessage("The text is not accepted");
    }
}
