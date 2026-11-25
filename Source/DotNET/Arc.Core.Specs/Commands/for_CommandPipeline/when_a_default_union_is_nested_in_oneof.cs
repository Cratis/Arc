// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_default_union_is_nested_in_oneof : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Wrapped(new Nested(OneOf<Answer, string>.FromT0(default))));

    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_handle_any_values() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), Arg.Any<object>());
}
#endif
