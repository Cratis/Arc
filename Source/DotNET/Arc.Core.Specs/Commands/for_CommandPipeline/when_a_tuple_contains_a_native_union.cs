// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_tuple_contains_a_native_union : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Tuple(_recorded, _receipt));

    [Fact] void should_return_the_unwrapped_element() => _result.ResponseValue.ShouldEqual(_receipt);
    [Fact] void should_handle_the_other_element_once() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _recorded);
}
#endif
