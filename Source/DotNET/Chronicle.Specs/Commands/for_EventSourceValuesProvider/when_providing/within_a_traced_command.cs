// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

public class within_a_traced_command : Specification
{
    EventSourceValuesProvider _provider;
    ActivitySource _source;
    ActivityListener _listener;
    Activity _span;
    Guid _id;

    void Establish()
    {
        _provider = new EventSourceValuesProvider(new RecordingLogger<EventSourceValuesProvider>());
        _id = Guid.NewGuid();
        _source = new ActivitySource(WellKnownDiagnostics.ActivitySourceName);
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
        _provider.Provide(new RegisterAuthor(new EventSourceId<Guid>(_id)));
        _span = span;
    }

    [Fact] void should_add_the_type_of_the_event_source_id() =>
        _span.GetTagItem("cratis.arc.command.event_source_id.type").ShouldEqual("Cratis.Chronicle.Events.EventSourceId<System.Guid>");
    [Fact] void should_not_add_the_event_source_id() =>
        _span.TagObjects.Any(_ => _.Value?.ToString()?.Contains(_id.ToString(), StringComparison.Ordinal) == true).ShouldBeFalse();

    record RegisterAuthor(EventSourceId<Guid> Id);
}
