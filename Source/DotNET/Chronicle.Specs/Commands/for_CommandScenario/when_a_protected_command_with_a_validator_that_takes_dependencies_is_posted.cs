// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

#pragma warning disable SA1402, SA1649

public class when_a_protected_command_with_a_validator_that_takes_dependencies_is_posted
{
    [Fact]
    public Task execute_endpoint_refuses_it_as_a_server_error_with_the_redacted_message_and_no_side_effects() =>
        Post(exposeExceptionDetails: false, validateOnly: false, (status, response, scenario) =>
        {
            status.ShouldEqual((int)HttpStatusCode.InternalServerError);
            response.IsSuccess.ShouldBeFalse();
            response.ExceptionMessages.Single().ShouldEqual(ExceptionDetailRedactor.RedactedMessage);
            ProtectedWithValidator.Handles.ShouldEqual(0);
            Assert.Empty(scenario.AppendedEvents);
        });

    [Fact]
    public Task validate_endpoint_refuses_it_the_same_way() =>
        Post(exposeExceptionDetails: false, validateOnly: true, (status, response, _) =>
        {
            status.ShouldEqual((int)HttpStatusCode.InternalServerError);
            response.ExceptionMessages.Single().ShouldEqual(ExceptionDetailRedactor.RedactedMessage);
            ProtectedWithValidator.Handles.ShouldEqual(0);
        });

    [Fact]
    public Task detailed_message_names_the_refused_validator_when_details_are_exposed() =>
        Post(exposeExceptionDetails: true, validateOnly: false, (status, response, _) =>
        {
            status.ShouldEqual((int)HttpStatusCode.InternalServerError);
            var message = response.ExceptionMessages.Single();
            Assert.Contains("Discoverable validator", message);
            Assert.Contains(nameof(ProtectedWithValidatorValidator), message);
            Assert.Contains("constructor takes dependencies", message);
            Assert.Contains("Arc#2831", message);
        });

    static async Task Post(bool exposeExceptionDetails, bool validateOnly, Action<int, CommandResult, CommandScenario<ProtectedWithValidator>> assert)
    {
        ProtectedWithValidator.Handles = 0;
        await using var scenario = new CommandScenario<ProtectedWithValidator>().UseDecisionReads();
        scenario.Services.Configure<ArcOptions>(options => options.ExposeExceptionDetails = exposeExceptionDetails);
        var command = new ProtectedWithValidator(EventSourceId.New());

        // Validation initializes the scenario's registrations; the endpoint then runs over the same provider.
        _ = await scenario.Validate(command);
        await using var provider = scenario.Services.BuildServiceProvider();
        var mapper = Substitute.For<IEndpointMapper>();
        mapper.MapCommandEndpoints(provider);
        var name = $"{(validateOnly ? "Validate" : "Execute")}{typeof(ProtectedWithValidator).FullName}";
        var endpoint = mapper.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IEndpointMapper.MapPost) &&
            call.GetArguments().OfType<EndpointMetadata>().Single().Name == name);
        var handler = (Func<IHttpRequestContext, Task>)endpoint.GetArguments()[1]!;
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(provider);
        context.Headers.Returns(new Dictionary<string, string>());
        context.ReadBodyAsJson(typeof(ProtectedWithValidator), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(command));
        var status = 0;
        context.When(_ => _.SetStatusCode(Arg.Any<int>())).Do(call => status = call.Arg<int>());
        CommandResult? response = null;
        context.WriteResponseAsJson(Arg.Do<object?>(value => response = (CommandResult)value!), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await handler(context);

        Assert.NotNull(response);
        assert(status, response, scenario);
    }

    [Command]
    [ProtectedDecision]
    public record ProtectedWithValidator(EventSourceId EventSourceId)
    {
        public static int Handles { get; set; }

        public when_using_decision_mode.DecisionFinished Handle(DecisionRead<when_using_decision_mode.DecisionState> read)
        {
            Handles++;
            return new(read.Exists);
        }
    }

    public class ProtectedWithValidatorValidator(TimeProvider clock) : CommandValidator<ProtectedWithValidator>
    {
        public TimeProvider Clock { get; } = clock;
    }
}
