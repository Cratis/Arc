// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_a_request_scope : given.a_certified_command
{
    CommandResult _response;

    async Task Because()
    {
        Build();
        var mapper = Substitute.For<IEndpointMapper>();
        mapper.MapCommandEndpoints(_root);
        var endpoint = mapper.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IEndpointMapper.MapPost) &&
            call.GetArguments().OfType<EndpointMetadata>().Single().Name == $"Execute{typeof(CertifiedCommand).FullName}");
        var handler = (Func<IHttpRequestContext, Task>)endpoint.GetArguments()[1]!;
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(_scope.ServiceProvider);
        request.Headers.Returns(new Dictionary<string, string>());
        request.ReadBodyAsJson(typeof(CertifiedCommand), Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(_command));
        request.WriteResponseAsJson(Arg.Do<object?>(value => _response = (CommandResult)value!), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        await handler(request);
    }

    [Fact] void should_accept_a_native_http_request_scope() => _response.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_the_command_event() => _scenario.AppendedEvents.Count.ShouldEqual(1);
}
