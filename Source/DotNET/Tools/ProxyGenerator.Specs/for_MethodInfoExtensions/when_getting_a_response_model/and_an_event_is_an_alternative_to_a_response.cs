// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_an_event_is_an_alternative_to_a_response : given.an_attributed_command_response
{
    void Because() => GenerateProxy(nameof(given.command_methods.ReturnsEventOrResponse));

    [Fact] void should_select_the_unconsumed_response() => _result.ResponseModel.Type.ShouldEqual(typeof(given.ClientResponse));
    [Fact] void should_generate_the_dto_response_type() => _proxy.ShouldContain("extends Command<IRegisterProject, ClientResponse>");
}
