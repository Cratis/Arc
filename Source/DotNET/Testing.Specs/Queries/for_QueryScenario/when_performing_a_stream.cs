// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_performing_a_stream : Specification
{
    readonly QueryScenario<StreamRejectionReadModel> _scenario = new();
    readonly StreamInvocationTracker _tracker = new();
    Exception? _exception;

    void Establish() => _scenario.Services.AddSingleton(_tracker);

    async Task Because()
    {
        try
        {
            await _scenario.Perform(nameof(StreamRejectionReadModel.AsyncStream));
        }
        catch (StreamingQueryNotSupported exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_reject_streaming_with_a_clear_message() => _exception!.Message.ShouldContain("use QueryScenario.Observe");
    [Fact] void should_not_invoke_the_query() => _tracker.Invocations.ShouldEqual(0);

    void Destroy() => _scenario.Dispose();
}
