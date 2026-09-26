// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_performing_a_stream : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    Exception? _exception;

    void Establish() => ScenarioReadModel.StreamInvocations = 0;

    async Task Because()
    {
        try
        {
            await _scenario.Perform(nameof(ScenarioReadModel.Stream));
        }
        catch (StreamingQueryNotSupported exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_reject_streaming_with_a_clear_message() => _exception!.Message.ShouldContain("snapshot queries only");
    [Fact] void should_not_invoke_the_query() => ScenarioReadModel.StreamInvocations.ShouldEqual(0);

    void Destroy() => _scenario.Dispose();
}
