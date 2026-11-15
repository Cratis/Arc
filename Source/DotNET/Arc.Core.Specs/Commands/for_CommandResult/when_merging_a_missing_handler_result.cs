// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_merging_a_missing_handler_result : Specification
{
    CommandResult _result;
    CommandResult<string> _typedResult;

    void Establish()
    {
        _result = CommandResult.Success(CorrelationId.New());
        _typedResult = CommandResult<string>.Success(CorrelationId.New());
    }

    void Because()
    {
        var missing = CommandResult.MissingHandler(CorrelationId.New(), typeof(string));
        _result.MergeWith(CommandResult.Success(CorrelationId.New()), missing);
        _typedResult.MergeWith(missing);
        _result.MergeWith(CommandResult.Success(CorrelationId.New()));
    }

    [Fact] void should_retain_the_missing_handler_flag() => _result.IsMissingHandler.ShouldBeTrue();
    [Fact] void should_merge_into_a_typed_result() => _typedResult.IsMissingHandler.ShouldBeTrue();
    [Fact] void should_still_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_still_be_unsuccessful() => _result.IsSuccess.ShouldBeFalse();
}
