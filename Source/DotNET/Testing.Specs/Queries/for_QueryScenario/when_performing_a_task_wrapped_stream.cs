// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_performing_a_task_wrapped_stream : Specification
{
    readonly QueryScenario<TaskStreamingReadModel> _scenario = new();
    Exception? _exception;

    async Task Because() => _exception = await Catch.Exception(() => _scenario.Perform(nameof(TaskStreamingReadModel.Stream)));

    [Fact] void should_reject_the_stream_before_invoking_the_query() => _exception.ShouldBeOfExactType<StreamingQueryNotSupported>();
    [Fact] void should_explain_how_to_observe_the_stream() => _exception!.Message.ShouldContain("use QueryScenario.Observe");

    void Destroy() => _scenario.Dispose();
}
