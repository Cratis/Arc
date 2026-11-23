// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_later_tuple_element_contains_a_default_union : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Tuple(_recorded, default));

    [Fact] void should_fail_before_handling_the_first_element() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), Arg.Any<object>());
    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
}
#endif
