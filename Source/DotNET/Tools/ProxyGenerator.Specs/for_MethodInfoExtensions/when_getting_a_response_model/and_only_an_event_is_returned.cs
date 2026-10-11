// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_only_an_event_is_returned : given.an_attributed_command_response
{
    void Because() => GenerateProxy(nameof(given.command_methods.ReturnsEvent));

    [Fact] void should_have_no_response() => _result.HasResponse.ShouldBeFalse();
    [Fact] void should_generate_a_command_without_a_response_type() => _proxy.ShouldContain("extends Command<IRegisterProject>");
}
