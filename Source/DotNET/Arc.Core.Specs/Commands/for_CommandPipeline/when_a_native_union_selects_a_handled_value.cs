// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_native_union_selects_a_handled_value : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Direct(_recorded));

    [Fact] void should_handle_the_active_case_once() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _recorded);
    [Fact] void should_succeed_without_a_response() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_return_the_handled_value() => _result.ResponseValue.ShouldBeNull();
}
#endif
