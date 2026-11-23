// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_native_union_selects_a_tuple : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Wrapped(new Nested((_receipt, _recorded))));

    [Fact] void should_return_the_unhandled_element() => _result.ResponseValue.ShouldEqual(_receipt);
    [Fact] void should_handle_the_other_element_once() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _recorded);
}
#endif
