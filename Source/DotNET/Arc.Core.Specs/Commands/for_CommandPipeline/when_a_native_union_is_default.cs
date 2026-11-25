// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_native_union_is_default : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Direct(default));

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_invalid_value() => _result.ExceptionMessages.ShouldContain("A command union must contain an active non-null value.");
    [Fact] void should_not_handle_any_values() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), Arg.Any<object>());
}
#endif
