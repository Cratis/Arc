// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

/// <summary>
/// The provider writes to the ambient span, so it has to be sure that span is the command span Arc started; any other
/// instrumentation's span is not its to write to.
/// </summary>
public class within_a_span_that_is_not_a_command : Specification
{
    EventSourceValuesProvider _provider;
    ActivitySource _source;
    ActivityListener _listener;
    Activity _span;

    void Establish()
    {
        _provider = new EventSourceValuesProvider(new RecordingLogger<EventSourceValuesProvider>());
        _source = new ActivitySource("Microsoft.AspNetCore");
        _listener = new ActivityListener
        {
            ShouldListenTo = candidate => ReferenceEquals(candidate, _source),
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    void Destroy()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    void Because()
    {
        using var span = _source.StartActivity("cratis.arc.command.execute")!;
        _provider.Provide(new RegisterAuthor(new EventSourceId<Guid>(Guid.NewGuid())));
        _span = span;
    }

    [Fact] void should_not_add_the_type_of_the_event_source_id() => _span.GetTagItem("cratis.arc.command.event_source_id.type").ShouldBeNull();

    record RegisterAuthor(EventSourceId<Guid> Id);
}
