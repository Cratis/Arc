// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_rejecting_an_invalid_raw_composite_event_source : given.a_keyed_protected_read
{
    Exception? _failure;

    void Establish()
    {
        var key = new CompositeKey(new("owner"), "*");
        _context = _context with { Type = typeof(SourceLookup), Command = new SourceLookup(key) };
        _context.Values[WellKnownCommandContextKeys.EventSourceId] = EventSourceExtensions.ToEventSourceId(key);
        _inner.ClearReceivedCalls();
        _stored.ClearReceivedCalls();
    }

    void Because() => _failure = Catch.Exception(() => ReadParameter(typeof(SourceLookup).GetMethod(nameof(SourceLookup.Handle))!.GetParameters()[0]));

    [Fact]
    void should_check_the_raw_composite_event_source_before_using_the_fallback_key()
    {
        AssertMissing(_failure!, string.Empty);
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
    }
}
