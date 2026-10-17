// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_receipt_support_is_unknown : given.an_occurrence_command_executor
{
    void Establish()
    {
        _capabilities.Unknown();
        CreateExecutor();
    }

    async Task Because() => _exception = await Catch.Exception(() => _executor.Execute(_command, _occurrence, "send-reminder"));

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<EffectReceiptsNotSupported>();
    [Fact] void should_not_run_the_command() => _pipelineCalls.ShouldEqual(0);
}
