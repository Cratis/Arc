// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_namespace_maps_to_another_tenant : given.an_occurrence_command_executor
{
    void Establish()
    {
        _occurrenceNamespace = new EventStoreNamespaceName("other");
        CreateExecutor();
    }

    async Task Because() => _exception = await Catch.Exception(() => _executor.Execute(_command, _occurrence, "send-reminder"));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<OccurrenceCommandNamespaceMismatch>();
    [Fact] void should_not_run_the_command() => _pipelineCalls.ShouldEqual(0);
}
