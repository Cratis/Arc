// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

public class and_the_default_stream_is_requested : Specification
{
    CommandScenario<CompleteDefaultStream> _scenario;
    CommandResult _result;

    void Establish() => _scenario = new();
    async Task Because() => _result = await _scenario.Execute(new(EventSourceId.New()));

    [Fact] void should_refuse_as_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_throw() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The default stream cannot be completed.");
    [Fact] void should_append_nothing() => _scenario.AppendedEvents.ShouldBeEmpty();
    [Fact] void should_complete_nothing() => _scenario.CompletedStreams.ShouldBeEmpty();
    void Destroy() => _scenario.Dispose();
}
