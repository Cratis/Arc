// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_native_union_selects_validation : given.a_native_union_pipeline
{
    async Task Because() => await Run(new Direct(ValidationResult.Error("Rejected", ["Name"])));

    [Fact] void should_reject_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_validation() => _result.ValidationResults.Single().Message.ShouldEqual("Rejected");
    [Fact] void should_not_return_a_response() => _result.ResponseValue.ShouldBeNull();
    [Fact] void should_not_handle_the_unselected_event() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), Arg.Any<Recorded>());
}
#endif
